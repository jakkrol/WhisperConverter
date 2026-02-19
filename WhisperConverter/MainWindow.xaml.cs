using Microsoft.Win32;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Whisper.net;
using Whisper.net.Ggml;

namespace WhisperConverter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //public static List<string> modes = new List<string>();
        //public string selectedModel = "";
        public MainWindow()
        {
            InitializeComponent();
            MyCombo.ItemsSource = new List<string> { "base", "medium", "large" };
            LaunguageCombo.ItemsSource = new List<string>{ "pl", "en" };
            MyCombo.SelectedIndex = 0;
            LaunguageCombo.SelectedIndex = 0;
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

                string selectedModel = MyCombo.SelectedItem.ToString();
                string modelPath = $"ggml-{selectedModel}.bin";
                //string modelPath = "ggml-medium.bin";

                TranscriptBox.Text = "Loading...";

                if (!File.Exists(modelPath))
                {
                    GgmlType type = selectedModel switch
                    {
                        "base" => GgmlType.Base,
                        "medium" => GgmlType.Medium,
                        "large" => GgmlType.LargeV1,
                        _ => GgmlType.Base
                    };
                    TranscriptBox.Text = $"Downloading {selectedModel} model...";
                    var modelStream = await WhisperGgmlDownloader.Default.GetGgmlModelAsync(type);
                    using var fileWriter = File.OpenWrite(modelPath);
                    await modelStream.CopyToAsync(fileWriter);
                    TranscriptBox.Text = "Model downloaded.";
                }
                else
                {
                    TranscriptBox.Text = "Model already downloaded.";
                }

                var fileInfo = new FileInfo(modelPath);
                TranscriptBox.Text += $"\nModel size: {fileInfo.Length / (1024 * 1024)} MB";


                TranscriptBox.Text += "\nTranskrypcja...";

                // Konwersja do 16kHz mono
                string tempWavPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "converted.wav");
                TranscriptBox.Text += "\nKonwersja audio (proszę czekać)...";
                await Task.Run(() => ConvertTo16kHzMono(wavPath, tempWavPath)); 
                TranscriptBox.Text += "\nKonwersja zakończona. Rozpoczynam transkrypcję...";


                //CREATING BUILDER
                var sb = new StringBuilder();
                var factory = WhisperFactory.FromPath(modelPath);
                //var processor = factory.CreateBuilder()
                //    .WithLanguage(LaunguageCombo.SelectedItem.ToString())
                //    .Build();
                var processor = factory.CreateBuilder()
                    .WithLanguage(LaunguageCombo.SelectedItem.ToString())
                    .Build();
               



                using var fs = File.OpenRead(tempWavPath);
                await foreach (var result in processor.ProcessAsync(fs))
                {
                    //sb.AppendLine($"{result.Start:hh\\:mm\\:ss} - {result.Text}");

                    //if (result.Start.TotalSeconds % 30 == 0) 
                    //{
                    //    TranscriptBox.Text = sb.ToString();
                    //    await Task.Delay(1); 
                    //}
                    sb.AppendLine($"{result.Start:hh\\:mm\\:ss} - {result.Text}");
                    TranscriptBox.Text = sb.ToString();
                    await Task.Delay(10); 
                }
                TranscriptBox.Text = sb.ToString();
            }
        }



        void ConvertTo16kHzMono(string inputPath, string outputPath)
        {
            //using var reader = new AudioFileReader(inputPath);

            //// 1. Convert stereo to mono
            //var mono = new StereoToMonoSampleProvider(reader)
            //{
            //    LeftVolume = 1.0f,
            //    RightVolume = 1.0f
            //};

            //// 2. Normalize audio to boost quieter speech
            //var normalized = new VolumeSampleProvider(mono)
            //{
            //    Volume = 2.0f
            //};

            //// 3. Apply a less aggressive noise gate
            //var gated = ApplyNoiseGate(normalized, -35);

            //// 4. Resample to 16kHz mono
            //var resampler = new WdlResamplingSampleProvider(gated, 16000);
            //WaveFileWriter.CreateWaveFile16(outputPath, resampler);

            //// 5. Info box (optional)
            //using var check = new AudioFileReader(outputPath);
            //MessageBox.Show($"Długość przekonwertowanego pliku: {check.TotalTime}");

            using (var reader = new AudioFileReader(inputPath)) 
            {
                var mono = new StereoToMonoSampleProvider(reader)
                {
                    LeftVolume = 1.0f,
                    RightVolume = 1.0f
                };

                var normalized = new VolumeSampleProvider(mono)
                {
                    Volume = 2.0f
                };

                var gated = ApplyNoiseGate(normalized, -35);

                var resampler = new WdlResamplingSampleProvider(gated, 16000);
                WaveFileWriter.CreateWaveFile16(outputPath, resampler);

                using var check = new AudioFileReader(outputPath);
                MessageBox.Show($"Długość przekonwertowanego pliku: {check.TotalTime}");
            }
        }


        ISampleProvider ApplyNoiseGate(ISampleProvider input, float thresholdDb = -30f)
        {
            return new NoiseGateSampleProvider(input, thresholdDb);
        }

        private void MyCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}