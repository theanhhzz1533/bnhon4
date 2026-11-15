using System;

namespace Bnhon.Game.Audio
{
    /// <summary>Names of the game's sound effects. Also the file names looked up in Resources/Audio.</summary>
    public static class SoundId
    {
        public const string PistolShot = "pistol_shot";
        public const string ShotgunShot = "shotgun_shot";
        public const string KnifeSwing = "knife_swing";
        public const string Reload = "reload";
        public const string EmptyClick = "empty_click";
        public const string Footstep = "footstep";
        public const string Roll = "roll";
        public const string PlayerHurt = "player_hurt";
        public const string PlayerDeath = "player_death";
        public const string ZombieGrowl = "zombie_growl";
        public const string ZombieScream = "zombie_scream";
        public const string ZombieHit = "zombie_hit";
        public const string ZombieDeath = "zombie_death";
        public const string LootOpen = "loot_open";
        public const string ItemReveal = "item_reveal";
        public const string UseItem = "use_item";
        public const string Extract = "extract";

        public static readonly string[] All =
        {
            PistolShot, ShotgunShot, KnifeSwing, Reload, EmptyClick, Footstep, Roll, PlayerHurt, PlayerDeath,
            ZombieGrowl, ZombieScream, ZombieHit, ZombieDeath, LootOpen, ItemReveal, UseItem, Extract
        };
    }

    /// <summary>
    /// Placeholder sound effects made from noise and sine waves, so the prototype has audio
    /// without any audio files. Each returns mono samples in the range -1..1.
    /// </summary>
    public static class SoundSynth
    {
        public const int SampleRate = 44100;
        private const double TwoPi = Math.PI * 2.0;

        /// <summary>Samples for a sound id, or null if the id is unknown.</summary>
        public static float[] Generate(string id)
        {
            // A fixed seed per sound: the same noise every run, so sounds do not change between sessions.
            var rng = new Random(StableHash(id));
            float[] samples;

            switch (id)
            {
                case SoundId.PistolShot: samples = Shot(rng, 0.22f, 110f, 2600f, 22f); break;
                case SoundId.ShotgunShot: samples = Shot(rng, 0.42f, 70f, 1500f, 11f); break;
                case SoundId.KnifeSwing: samples = Swoosh(rng, 0.14f, 3200f); break;
                case SoundId.Roll: samples = Swoosh(rng, 0.26f, 1200f); break;
                case SoundId.Reload: samples = Clicks(rng, 0.42f, new[] { 0.02f, 0.2f, 0.33f }); break;
                case SoundId.EmptyClick: samples = Clicks(rng, 0.08f, new[] { 0.01f }); break;
                case SoundId.Footstep: samples = Thud(rng, 0.07f, 95f, 0.5f); break;
                case SoundId.ZombieHit: samples = Thud(rng, 0.1f, 150f, 0.8f); break;
                case SoundId.PlayerHurt: samples = Sweep(rng, 0.18f, 340f, 170f, 0.25f, 0f); break;
                case SoundId.PlayerDeath: samples = Sweep(rng, 0.9f, 260f, 55f, 0.2f, 5f); break;
                case SoundId.ZombieDeath: samples = Sweep(rng, 0.45f, 190f, 50f, 0.5f, 18f); break;
                case SoundId.ZombieGrowl: samples = Growl(rng, 0.7f); break;
                case SoundId.ZombieScream: samples = Scream(rng, 1.0f); break;
                case SoundId.LootOpen: samples = Sweep(rng, 0.2f, 180f, 420f, 0.35f, 30f); break;
                case SoundId.ItemReveal: samples = Tones(0.09f, new[] { 880f }); break;
                case SoundId.UseItem: samples = Tones(0.22f, new[] { 392f, 523f }); break;
                case SoundId.Extract: samples = Tones(0.6f, new[] { 392f, 523f, 659f, 784f }); break;
                default: return null;
            }

            Normalize(samples, 0.85f);
            return samples;
        }

        /// <summary>Gunshot: a burst of noise that dies away fast, over a low thump.</summary>
        private static float[] Shot(Random rng, float seconds, float thumpHz, float cutoffHz, float decay)
        {
            var samples = new float[(int)(seconds * SampleRate)];
            float low = 0f;
            float alpha = LowPassAlpha(cutoffHz);
            for (int i = 0; i < samples.Length; i++)
            {
                float t = (float)i / SampleRate;
                low += alpha * (Noise(rng) - low);
                float crack = low * (float)Math.Exp(-decay * t);
                float thump = (float)Math.Sin(TwoPi * thumpHz * t) * (float)Math.Exp(-decay * 0.6f * t);
                samples[i] = crack + thump * 0.7f;
            }
            return samples;
        }

        /// <summary>Air noise that swells and fades: a blade or a body moving fast.</summary>
        private static float[] Swoosh(Random rng, float seconds, float cutoffHz)
        {
            var samples = new float[(int)(seconds * SampleRate)];
            float low = 0f;
            float alpha = LowPassAlpha(cutoffHz);
            for (int i = 0; i < samples.Length; i++)
            {
                float u = (float)i / samples.Length;
                low += alpha * (Noise(rng) - low);
                samples[i] = low * (float)Math.Sin(Math.PI * u);
            }
            return samples;
        }

        /// <summary>Short mechanical ticks at the given times.</summary>
        private static float[] Clicks(Random rng, float seconds, float[] times)
        {
            var samples = new float[(int)(seconds * SampleRate)];
            foreach (float start in times)
            {
                int first = (int)(start * SampleRate);
                int length = (int)(0.03f * SampleRate);
                for (int i = 0; i < length && first + i < samples.Length; i++)
                {
                    float t = (float)i / SampleRate;
                    float tick = Noise(rng) * 0.6f + (float)Math.Sin(TwoPi * 1800f * t) * 0.4f;
                    samples[first + i] += tick * (float)Math.Exp(-160f * t);
                }
            }
            return samples;
        }

        /// <summary>A dull impact: low tone plus muffled noise.</summary>
        private static float[] Thud(Random rng, float seconds, float hz, float noiseAmount)
        {
            var samples = new float[(int)(seconds * SampleRate)];
            float low = 0f;
            float alpha = LowPassAlpha(700f);
            for (int i = 0; i < samples.Length; i++)
            {
                float t = (float)i / SampleRate;
                low += alpha * (Noise(rng) - low);
                float envelope = (float)Math.Exp(-45f * t);
                samples[i] = ((float)Math.Sin(TwoPi * hz * t) + low * noiseAmount * 3f) * envelope;
            }
            return samples;
        }

        /// <summary>A tone gliding from one pitch to another, with optional grit and wobble.</summary>
        private static float[] Sweep(Random rng, float seconds, float fromHz, float toHz, float noiseAmount, float wobbleHz)
        {
            var samples = new float[(int)(seconds * SampleRate)];
            double phase = 0.0;
            for (int i = 0; i < samples.Length; i++)
            {
                float u = (float)i / samples.Length;
                float t = (float)i / SampleRate;
                float hz = fromHz + (toHz - fromHz) * u;
                if (wobbleHz > 0f) hz *= 1f + 0.08f * (float)Math.Sin(TwoPi * wobbleHz * t);
                phase += TwoPi * hz / SampleRate;

                float envelope = (float)Math.Sin(Math.PI * Math.Min(1f, u * 6f) * 0.5f) * (1f - u);
                samples[i] = ((float)Math.Sin(phase) + Noise(rng) * noiseAmount) * envelope;
            }
            return samples;
        }

        /// <summary>Low, rough and unsteady: something breathing in the dark.</summary>
        private static float[] Growl(Random rng, float seconds)
        {
            var samples = new float[(int)(seconds * SampleRate)];
            double phase = 0.0;
            float low = 0f;
            float alpha = LowPassAlpha(500f);
            for (int i = 0; i < samples.Length; i++)
            {
                float u = (float)i / samples.Length;
                float t = (float)i / SampleRate;
                float hz = 72f + 14f * (float)Math.Sin(TwoPi * 3.1f * t);
                phase += TwoPi * hz / SampleRate;

                // A sawtooth is harsher than a sine; the tremolo makes it rattle.
                float saw = (float)(phase / TwoPi % 1.0) * 2f - 1f;
                float tremolo = 0.6f + 0.4f * (float)Math.Sin(TwoPi * 23f * t);
                low += alpha * (Noise(rng) - low);
                samples[i] = (saw * tremolo + low * 2f) * (float)Math.Sin(Math.PI * u);
            }
            return samples;
        }

        /// <summary>A high wavering shriek that carries.</summary>
        private static float[] Scream(Random rng, float seconds)
        {
            var samples = new float[(int)(seconds * SampleRate)];
            double phase = 0.0;
            for (int i = 0; i < samples.Length; i++)
            {
                float u = (float)i / samples.Length;
                float t = (float)i / SampleRate;
                float hz = 520f + 420f * (float)Math.Sin(Math.PI * u) + 40f * (float)Math.Sin(TwoPi * 13f * t);
                phase += TwoPi * hz / SampleRate;

                float voice = (float)Math.Sin(phase) + 0.5f * (float)Math.Sin(phase * 2.0) + 0.25f * (float)Math.Sin(phase * 3.0);
                float envelope = Math.Min(1f, u * 12f) * (float)Math.Pow(1f - u, 0.6);
                samples[i] = (voice + Noise(rng) * 0.35f) * envelope;
            }
            return samples;
        }

        /// <summary>Clean notes played one after another: interface feedback.</summary>
        private static float[] Tones(float seconds, float[] notes)
        {
            var samples = new float[(int)(seconds * SampleRate)];
            int perNote = samples.Length / notes.Length;
            for (int n = 0; n < notes.Length; n++)
            {
                for (int i = 0; i < perNote; i++)
                {
                    float t = (float)i / SampleRate;
                    float u = (float)i / perNote;
                    float envelope = Math.Min(1f, u * 20f) * (1f - u);
                    samples[n * perNote + i] = (float)Math.Sin(TwoPi * notes[n] * t) * envelope;
                }
            }
            return samples;
        }

        private static float Noise(Random rng)
        {
            return (float)(rng.NextDouble() * 2.0 - 1.0);
        }

        private static float LowPassAlpha(float cutoffHz)
        {
            double rc = 1.0 / (TwoPi * cutoffHz);
            double dt = 1.0 / SampleRate;
            return (float)(dt / (rc + dt));
        }

        private static void Normalize(float[] samples, float peak)
        {
            float max = 0f;
            foreach (float sample in samples) max = Math.Max(max, Math.Abs(sample));
            if (max < 0.0001f) return;
            float gain = peak / max;
            for (int i = 0; i < samples.Length; i++) samples[i] *= gain;
        }

        /// <summary>string.GetHashCode changes between runs on .NET; this does not.</summary>
        private static int StableHash(string text)
        {
            unchecked
            {
                int hash = 23;
                foreach (char c in text) hash = hash * 31 + c;
                return hash;
            }
        }
    }
}
