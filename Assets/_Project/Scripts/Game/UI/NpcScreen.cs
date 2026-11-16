using System.Collections.Generic;
using Bnhon.Core;
using Bnhon.Core.Content;
using Bnhon.Core.Inventory;
using Bnhon.Core.Items;
using Bnhon.Core.Npcs;
using Bnhon.Core.Quests;
using Bnhon.Core.Relationships;
using Bnhon.Game.Audio;
using UnityEngine;

namespace Bnhon.Game.UI
{
    /// <summary>
    /// Talking to a resident: today's line, gifts chosen from the backpack, their quests,
    /// asking them out, and their heart scenes, which play here one line at a time.
    /// </summary>
    public sealed class NpcScreen : ModalScreen
    {
        private enum Mode
        {
            Talk,
            Gift,
            Scene
        }

        private static readonly Color Dim = new Color(0.7f, 0.7f, 0.72f);
        private static readonly Color Heart = new Color(0.95f, 0.45f, 0.55f);
        private static readonly Color Gold = new Color(0.95f, 0.85f, 0.4f);

        private string _npcId;
        private NpcLife _life;
        private Mode _mode;
        private string _speech;
        private Vector2 _scroll;

        private HeartEvent _scene;
        private int _sceneLine;
        private SceneChoice _sceneChoice;

        public static NpcScreen Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (Instance == this) Instance = null;
        }

        /// <summary>Opens the screen on a resident. A waiting heart scene starts at once; otherwise they say today's line.</summary>
        public void Show(string npcId, string spot)
        {
            GameSession session = GameRoot.Instance.Session;
            Relationship relationship = session.Relationships.Get(npcId);
            NpcLife life = PrototypeContent.FindNpcLife(npcId);
            if (relationship == null || life == null) return;

            _npcId = npcId;
            _life = life;
            _mode = Mode.Talk;
            _scene = null;
            _sceneChoice = null;

            if (!session.Story.Has(life.MetFlag))
            {
                session.Story.Set(life.MetFlag);
                session.Relationships.Talk(npcId);
                _speech = Fill(life.Dialogue.FirstMeeting);
            }
            else
            {
                HeartEvent waiting = life.NextEvent(relationship.Hearts, session.Phase, session.Story);
                if (waiting != null)
                {
                    StartScene(waiting);
                }
                else if (session.Relationships.Talk(npcId))
                {
                    _speech = Fill(life.Dialogue.Pick(relationship.Hearts, session.Phase, spot, session.Clock.TotalDays));
                }
                else
                {
                    _speech = Fill(life.Dialogue.AlreadyTalked);
                }
            }

            Open();
        }

        protected override void DrawContents()
        {
            GameSession session = GameRoot.Instance.Session;
            Relationship relationship = session.Relationships.Get(_npcId);
            if (relationship == null || _life == null) return;

            DrawHeader(relationship);
            GUILayout.Space(10f);

            if (_mode == Mode.Scene) DrawScene(session);
            else if (_mode == Mode.Gift) DrawGifts(session, relationship);
            else DrawTalk(session, relationship);
        }

        private void DrawHeader(Relationship relationship)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(relationship.Npc.DisplayName, TitleStyle);
            GUILayout.FlexibleSpace();

            int into = relationship.Points % RelationshipTracker.PointsPerHeart;
            string status = relationship.Status == RelationshipStatus.Dating ? "  ·  đang hẹn hò"
                : relationship.Status == RelationshipStatus.Married ? "  ·  bạn đời" : string.Empty;
            ColoredLabel("Tim " + relationship.Hearts + "/" + RelationshipTracker.MaxHearts
                + "  (" + into + "/" + RelationshipTracker.PointsPerHeart + ")" + status, Heart);
            GUILayout.EndHorizontal();
        }

        private void DrawTalk(GameSession session, Relationship relationship)
        {
            GUILayout.Label(_speech ?? string.Empty, TextStyle);
            GUILayout.Space(14f);

            bool canGift = !relationship.GiftedToday && relationship.GiftsThisWeek < RelationshipTracker.GiftsPerWeek;
            GUILayout.BeginHorizontal();
            if (Button("Tặng quà", canGift, 150f)) Defer(() => { _mode = Mode.Gift; _scroll = Vector2.zero; });
            ColoredLabel(relationship.GiftedToday ? "Hôm nay đã tặng rồi."
                : !canGift ? "Tuần này đã tặng đủ " + RelationshipTracker.GiftsPerWeek + " món."
                : "Mỗi ngày 1 món, mỗi tuần " + RelationshipTracker.GiftsPerWeek + " món. Tuần này đã tặng " + relationship.GiftsThisWeek + ".", Dim);
            GUILayout.EndHorizontal();

            if (session.Relationships.CanAskOut(_npcId))
            {
                GUILayout.Space(4f);
                GUILayout.BeginHorizontal();
                if (Button("Ngỏ lời hẹn hò", true, 150f))
                {
                    Defer(() =>
                    {
                        if (!session.Relationships.AskOut(_npcId)) return;
                        _speech = Fill(_life.Dialogue.AcceptsDating);
                        Sfx.PlayUi(SoundId.Extract, 0.6f);
                    });
                }
                ColoredLabel("Đã đủ thân để ngỏ lời.", Heart);
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(12f);
            DrawQuests(session);
        }

        private void DrawQuests(GameSession session)
        {
            bool any = false;
            foreach (DeliveryQuest quest in session.Quests.All)
            {
                if (quest.GiverNpcId != _npcId || quest.State == QuestState.Completed) continue;
                if (!any) GUILayout.Label("Nhiệm vụ", HeaderStyle);
                any = true;

                ItemDefinition item = PrototypeContent.FindItem(quest.ItemId);
                string itemName = item != null ? item.DisplayName : quest.ItemId;
                string reward = "thưởng " + quest.RewardMoney + " tem phiếu";

                GUILayout.BeginHorizontal();
                DeliveryQuest target = quest;
                if (quest.State == QuestState.Available)
                {
                    ColoredLabel(quest.Title + ": mang về " + quest.Count + " " + itemName + "  ·  " + reward, Color.white);
                    if (Button("Nhận", true, 110f))
                    {
                        Defer(() =>
                        {
                            session.Quests.Accept(target.Id);
                            Sfx.PlayUi(SoundId.ItemReveal, 0.5f);
                        });
                    }
                }
                else
                {
                    int have = session.Backpack.CountOf(quest.ItemId);
                    bool ready = session.Quests.CanTurnIn(quest.Id, session.Backpack);
                    ColoredLabel(quest.Title + ": " + itemName + " " + have + "/" + quest.Count + " (trong balo)  ·  " + reward, ready ? Gold : Color.white);
                    if (Button("Giao", ready, 110f))
                    {
                        Defer(() =>
                        {
                            if (!session.Quests.TurnIn(target.Id, session.Backpack, session.Wallet, session.Relationships)) return;
                            _speech = "Xong \"" + target.Title + "\". Nhận " + target.RewardMoney + " tem phiếu.";
                            Sfx.PlayUi(SoundId.Extract, 0.6f);
                        });
                    }
                }
                GUILayout.EndHorizontal();
            }
        }

        private void DrawGifts(GameSession session, Relationship relationship)
        {
            GUILayout.Label("Chọn một món trong balo để tặng " + relationship.Npc.DisplayName, HeaderStyle);
            _scroll = GUILayout.BeginScrollView(_scroll);

            // One row per kind of item, however many stacks it is split into.
            var seen = new HashSet<string>();
            foreach (PlacedItem placed in session.Backpack.Items)
            {
                if (!seen.Add(placed.Item.Id)) continue;

                GUILayout.BeginHorizontal();
                ColoredLabel(placed.Item.DisplayName + "  (có " + session.Backpack.CountOf(placed.Item.Id) + ")", Color.white, GUILayout.Width(320f));
                PlacedItem target = placed;
                if (Button("Tặng", true, 110f)) Defer(() => Give(session, target));
                GUILayout.EndHorizontal();
            }
            if (seen.Count == 0) ColoredLabel("Balo trống. Lấy quà từ kho bỏ vào balo trước.", Dim);

            GUILayout.EndScrollView();
            if (Button("Quay lại", true, 110f)) Defer(() => _mode = Mode.Talk);
        }

        private void Give(GameSession session, PlacedItem item)
        {
            string itemName = item.Item.DisplayName;
            GiftResult result = session.GiveGift(_npcId, session.Backpack, item, out GiftTaste taste);
            _mode = Mode.Talk;
            if (result != GiftResult.Accepted) return;

            int points = RelationshipTracker.PointsFor(taste);
            _speech = Fill(_life.Dialogue.GiftReaction(taste)) + "\n\n(Tặng " + itemName + ": " + (points >= 0 ? "+" : string.Empty) + points + " điểm)";
            Sfx.PlayUi(taste == GiftTaste.Disliked ? SoundId.Reload : SoundId.ItemReveal, 0.6f);
        }

        private void StartScene(HeartEvent scene)
        {
            _mode = Mode.Scene;
            _scene = scene;
            _sceneLine = 0;
            _sceneChoice = null;
        }

        private void DrawScene(GameSession session)
        {
            ColoredLabel("♥ " + _scene.Title, Heart);
            GUILayout.Space(8f);

            // Everything said so far stays on screen, so the scene reads like a page.
            int shown = Mathf.Min(_sceneLine, _scene.Lines.Count - 1);
            for (int i = 0; i <= shown; i++)
            {
                SceneLine line = _scene.Lines[i];
                if (line.Speaker == null) ColoredLabel(Fill(line.Text), Dim);
                else GUILayout.Label(line.Speaker + ": " + Fill(line.Text), TextStyle);
                GUILayout.Space(4f);
            }

            if (_sceneLine < _scene.Lines.Count - 1)
            {
                if (Button("Tiếp", true, 110f)) Defer(() => _sceneLine++);
                return;
            }

            GUILayout.Space(8f);
            if (_sceneChoice == null)
            {
                foreach (SceneChoice choice in _scene.Choices)
                {
                    SceneChoice picked = choice;
                    if (Button(Fill(choice.Text), true, 620f))
                    {
                        Defer(() =>
                        {
                            _sceneChoice = picked;
                            session.CompleteEvent(_npcId, _scene, picked);
                            GameRoot.Instance.Save(session.IsInRaid);
                            Sfx.PlayUi(SoundId.ItemReveal, 0.6f);
                        });
                    }
                }
                return;
            }

            ColoredLabel("Bạn: " + Fill(_sceneChoice.Text), Gold);
            GUILayout.Space(4f);
            GUILayout.Label(session.Relationships.Get(_npcId).Npc.DisplayName + ": " + Fill(_sceneChoice.Reply), TextStyle);
            GUILayout.Space(4f);
            ColoredLabel("(+" + _sceneChoice.Points + " điểm)", Dim);
            if (Button("Xong", true, 110f))
            {
                Defer(() =>
                {
                    _mode = Mode.Talk;
                    _speech = string.Empty;
                });
            }
        }

        private string Fill(string text)
        {
            Gender player = GameRoot.Instance.Session.Relationships.PlayerGender;
            return DialogueBook.Format(text, _life.Address(player));
        }
    }
}
