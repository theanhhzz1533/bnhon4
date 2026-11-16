using System;
using System.Collections.Generic;
using Bnhon.Core.Relationships;

namespace Bnhon.Core.Npcs
{
    /// <summary>The two halves of a day at the base: before the day's raid and after it.</summary>
    public enum DayPhase
    {
        Morning,
        Evening
    }

    /// <summary>
    /// Where a resident is, by day of the week and half of the day. A rule for a specific
    /// day wins over a rule for every day; with no rule the resident is at their usual spot.
    /// </summary>
    public sealed class NpcSchedule
    {
        private struct Rule
        {
            public int DayOfWeek;
            public DayPhase Phase;
            public string Spot;
        }

        private const int AnyDay = -1;
        private readonly List<Rule> _rules = new List<Rule>();

        public string DefaultSpot { get; }

        public NpcSchedule(string defaultSpot)
        {
            if (string.IsNullOrEmpty(defaultSpot)) throw new ArgumentException("A default spot is required.", nameof(defaultSpot));
            DefaultSpot = defaultSpot;
        }

        public NpcSchedule On(DayPhase phase, string spot)
        {
            _rules.Add(new Rule { DayOfWeek = AnyDay, Phase = phase, Spot = spot });
            return this;
        }

        public NpcSchedule OnDay(int dayOfWeek, DayPhase phase, string spot)
        {
            _rules.Add(new Rule { DayOfWeek = dayOfWeek, Phase = phase, Spot = spot });
            return this;
        }

        public string SpotFor(int dayOfWeek, DayPhase phase)
        {
            string everyDay = null;
            foreach (Rule rule in _rules)
            {
                if (rule.Phase != phase) continue;
                if (rule.DayOfWeek == dayOfWeek) return rule.Spot;
                if (rule.DayOfWeek == AnyDay && everyDay == null) everyDay = rule.Spot;
            }
            return everyDay ?? DefaultSpot;
        }

        /// <summary>Every spot this schedule can send the resident to.</summary>
        public IEnumerable<string> AllSpots()
        {
            yield return DefaultSpot;
            foreach (Rule rule in _rules) yield return rule.Spot;
        }
    }

    /// <summary>
    /// What a resident says in daily talk. Lines are picked by how close the player is to
    /// them, the half of the day and where they are standing; the pick is fixed for a day.
    /// Texts may use {p} and {P} for how the resident addresses the player.
    /// </summary>
    public sealed class DialogueBook
    {
        private sealed class Line
        {
            public int MinHearts;
            public int MaxHearts;
            public bool AnyPhase;
            public DayPhase Phase;
            public string Spot;
            public string Text;
        }

        private readonly List<Line> _lines = new List<Line>();
        private readonly Dictionary<GiftTaste, string> _giftReactions = new Dictionary<GiftTaste, string>();

        public string FirstMeeting { get; set; } = "Chào.";
        public string AlreadyTalked { get; set; } = "Mình nói chuyện rồi mà.";
        public string AcceptsDating { get; set; } = "...Vâng.";

        public DialogueBook Say(string text, int minHearts = 0, int maxHearts = RelationshipTracker.MaxHearts)
        {
            _lines.Add(new Line { MinHearts = minHearts, MaxHearts = maxHearts, AnyPhase = true, Text = text });
            return this;
        }

        public DialogueBook SayIn(DayPhase phase, string text, int minHearts = 0, int maxHearts = RelationshipTracker.MaxHearts)
        {
            _lines.Add(new Line { MinHearts = minHearts, MaxHearts = maxHearts, Phase = phase, Text = text });
            return this;
        }

        /// <summary>A line only said while standing at one spot. It is preferred over other lines there.</summary>
        public DialogueBook SayAt(string spot, string text, int minHearts = 0)
        {
            _lines.Add(new Line { MinHearts = minHearts, MaxHearts = RelationshipTracker.MaxHearts, AnyPhase = true, Spot = spot, Text = text });
            return this;
        }

        public DialogueBook OnGift(GiftTaste taste, string text)
        {
            _giftReactions[taste] = text;
            return this;
        }

        public string GiftReaction(GiftTaste taste)
        {
            return _giftReactions.TryGetValue(taste, out string text) ? text : "Cảm ơn.";
        }

        /// <summary>Today's line, or null if the book has nothing that fits.</summary>
        public string Pick(int hearts, DayPhase phase, string spot, int day)
        {
            var fitting = new List<Line>();
            var atSpot = new List<Line>();
            foreach (Line line in _lines)
            {
                if (hearts < line.MinHearts || hearts > line.MaxHearts) continue;
                if (!line.AnyPhase && line.Phase != phase) continue;
                if (line.Spot != null)
                {
                    if (line.Spot == spot) atSpot.Add(line);
                    continue;
                }
                fitting.Add(line);
            }

            List<Line> pool = atSpot.Count > 0 ? atSpot : fitting;
            if (pool.Count == 0) return null;
            return pool[Math.Abs(day) % pool.Count].Text;
        }

        /// <summary>Fills in how the resident addresses the player: {p} mid-sentence, {P} at the start of one.</summary>
        public static string Format(string text, string address)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(address)) return text;
            string capital = char.ToUpperInvariant(address[0]) + address.Substring(1);
            return text.Replace("{p}", address).Replace("{P}", capital);
        }
    }

    public sealed class SceneLine
    {
        /// <summary>Null for narration.</summary>
        public string Speaker { get; }
        public string Text { get; }

        public SceneLine(string speaker, string text)
        {
            Speaker = speaker;
            Text = text;
        }
    }

    public sealed class SceneChoice
    {
        public string Text { get; }
        public int Points { get; }
        public string Reply { get; }

        public SceneChoice(string text, int points, string reply)
        {
            Text = text;
            Points = points;
            Reply = reply;
        }
    }

    /// <summary>A short scene with a resident, unlocked at a number of hearts and seen once. It ends on a choice.</summary>
    public sealed class HeartEvent
    {
        private readonly List<SceneLine> _lines = new List<SceneLine>();
        private readonly List<SceneChoice> _choices = new List<SceneChoice>();

        public string Id { get; }
        public string Title { get; }
        public int Hearts { get; }

        /// <summary>False if the scene can happen in either half of the day.</summary>
        public bool HasPhase { get; }
        public DayPhase Phase { get; }

        public IReadOnlyList<SceneLine> Lines => _lines;
        public IReadOnlyList<SceneChoice> Choices => _choices;

        public HeartEvent(string id, string title, int hearts)
        {
            Id = id;
            Title = title;
            Hearts = hearts;
        }

        public HeartEvent(string id, string title, int hearts, DayPhase phase) : this(id, title, hearts)
        {
            HasPhase = true;
            Phase = phase;
        }

        public HeartEvent Narrate(string text) { _lines.Add(new SceneLine(null, text)); return this; }
        public HeartEvent Says(string speaker, string text) { _lines.Add(new SceneLine(speaker, text)); return this; }
        public HeartEvent Choice(string text, int points, string reply) { _choices.Add(new SceneChoice(text, points, reply)); return this; }
    }

    /// <summary>Things that have happened once in a save: residents met, scenes seen.</summary>
    public sealed class StoryFlags
    {
        private readonly HashSet<string> _flags = new HashSet<string>();

        public IEnumerable<string> All => _flags;

        public bool Has(string flag) => flag != null && _flags.Contains(flag);

        public void Set(string flag)
        {
            if (!string.IsNullOrEmpty(flag)) _flags.Add(flag);
        }

        public void Restore(IEnumerable<string> flags)
        {
            _flags.Clear();
            if (flags == null) return;
            foreach (string flag in flags) Set(flag);
        }
    }

    /// <summary>Everything that makes one resident a person: where they go, what they say, their scenes.</summary>
    public sealed class NpcLife
    {
        private readonly List<HeartEvent> _events = new List<HeartEvent>();

        public string NpcId { get; }
        public NpcSchedule Schedule { get; }
        public DialogueBook Dialogue { get; }
        public IReadOnlyList<HeartEvent> Events => _events;

        /// <summary>How this resident addresses a male and a female player ("anh" and "chị", say).</summary>
        public string AddressMale { get; set; } = "bạn";
        public string AddressFemale { get; set; } = "bạn";

        public NpcLife(string npcId, NpcSchedule schedule, DialogueBook dialogue)
        {
            NpcId = npcId;
            Schedule = schedule ?? throw new ArgumentNullException(nameof(schedule));
            Dialogue = dialogue ?? throw new ArgumentNullException(nameof(dialogue));
        }

        public NpcLife Add(HeartEvent heartEvent)
        {
            _events.Add(heartEvent);
            return this;
        }

        public string MetFlag => "met_" + NpcId;

        public string Address(Gender player) => player == Gender.Male ? AddressMale : AddressFemale;

        /// <summary>The lowest-heart scene that is unlocked, fits this half of the day and has not been seen. Null if none.</summary>
        public HeartEvent NextEvent(int hearts, DayPhase phase, StoryFlags flags)
        {
            HeartEvent best = null;
            foreach (HeartEvent candidate in _events)
            {
                if (hearts < candidate.Hearts || flags.Has(candidate.Id)) continue;
                if (candidate.HasPhase && candidate.Phase != phase) continue;
                if (best == null || candidate.Hearts < best.Hearts) best = candidate;
            }
            return best;
        }
    }
}
