using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WhisperConverter
{
    class NoiseGateSampleProvider : ISampleProvider
    {
        private readonly ISampleProvider source;
        private readonly float threshold;
        private readonly int channels;

        public NoiseGateSampleProvider(ISampleProvider source, float thresholdDb)
        {
            this.source = source;
            this.channels = source.WaveFormat.Channels;
            this.threshold = (float)Math.Pow(10.0, thresholdDb / 20.0); // dB → linear
        }

        public WaveFormat WaveFormat => source.WaveFormat;

        public int Read(float[] buffer, int offset, int count)
        {
            int samplesRead = source.Read(buffer, offset, count);

            for (int n = 0; n < samplesRead; n += channels)
            {
                float max = 0;
                for (int ch = 0; ch < channels; ch++)
                {
                    float sample = Math.Abs(buffer[offset + n + ch]);
                    if (sample > max) max = sample;
                }

                if (max > threshold)
                {
                    // zbyt głośno — przycisz
                    for (int ch = 0; ch < channels; ch++)
                    {
                        buffer[offset + n + ch] *= 0.3f;
                    }
                }
            }

            return samplesRead;
        }
    }

}
