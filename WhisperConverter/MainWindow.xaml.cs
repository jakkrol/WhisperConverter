﻿using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Whisper.net;
using Whisper.net.Ggml;
using System.ComponentModel;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace WhisperConverter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {

                Title = "Select an audio file"
            };

            if (dialog.ShowDialog() == true)
            {
                string wavPath = dialog.FileName;
                string modelPath = "ggml-base1.bin";

                TranscriptBox.Text = "Loading...";

                if (!File.Exists(modelPath))
                {
                    var modelStream = await WhisperGgmlDownloader.Default.GetGgmlModelAsync(GgmlType.Base);
                    using (var fileWriter = File.OpenWrite(modelPath))
                    {
                        await modelStream.CopyToAsync(fileWriter);
                    }
                }

                TranscriptBox.Text += "Transkrypcja...";

                // Konwersja do 16kHz mono
                string tempWavPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "converted.wav");
                ConvertTo16kHzMono(wavPath, tempWavPath);

                var sb = new StringBuilder();
                var factory = WhisperFactory.FromPath(modelPath);
                var processor = factory.CreateBuilder()
                    .WithLanguage("pl")
                    .Build();

                using var fs = File.OpenRead(tempWavPath);
                await foreach (var result in processor.ProcessAsync(fs))
                {
                    //sb.AppendLine($"{result.Start:hh\\:mm\\:ss} - {result.Text}");

                    //if (result.Start.TotalSeconds % 30 == 0) // co 30 sek
                    //{
                    //    TranscriptBox.Text = sb.ToString();
                    //    await Task.Delay(1); // odśwież UI
                    //}
                    sb.AppendLine($"{result.Start:hh\\:mm\\:ss} - {result.Text}");
                    TranscriptBox.Text = sb.ToString();
                    await Task.Delay(10); // żeby odświeżyć UI
                }
                TranscriptBox.Text = sb.ToString();
            }
        }



        void ConvertTo16kHzMono(string inputPath, string outputPath)
        {
            using var reader = new AudioFileReader(inputPath);

            var mono = new StereoToMonoSampleProvider(reader)
            {
                LeftVolume = 1.0f,
                RightVolume = 1.0f
            };

            // szum (oklaski) gate
            var gated = ApplyNoiseGate(mono, -25); // -25 dB to próg, można zmieniać

            // resample
            var resampler = new WdlResamplingSampleProvider(gated, 16000);
            WaveFileWriter.CreateWaveFile16(outputPath, resampler);

            // Sprawdzenie długości
            using var check = new AudioFileReader(outputPath);
            MessageBox.Show($"Długość przekonwertowanego pliku: {check.TotalTime}");
        }

        ISampleProvider ApplyNoiseGate(ISampleProvider input, float thresholdDb = -30f)
        {
            return new NoiseGateSampleProvider(input, thresholdDb);
        }


    }
}