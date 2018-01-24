using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;

namespace EncodeH265
{

    class Program
    {
        static string MainDir = "D:\\TV";

        static string FinalExtention = ".mkv";

        static bool Freeze = false;

        static string[] FileFormats =
        {
            "*.3gp",
            "*.asf",
            "*.wmv",
            "*.avi",
            "*.mkv",
            "*.mov",
            "*.mp4",
            "*.vob",
            "*.ts"
        };

        static void Main(string[] args)
        {
            for (int i = 0; i < args.Length; ++i)
            {
                switch (args[i])
                {
                    case "-dir":
                        MainDir = args[i + 1];
                        break;

                    case "-extout":
                        FinalExtention = args[i + 1];
                        break;

                    case "-freeze":
                        Freeze = true;
                        break;

                    case "-h":
                        Console.WriteLine("EncodeH265");
                        Console.WriteLine("Application written by Lukasz Bialowas");
                        Console.WriteLine();
                        Console.WriteLine("Application can be used to scan throught a directory for all video files. It will re-encode a non-h265 file into h265. If all files are already h265 or no video file is found, the application simply exits. Needs ffmpeg and ffprobe.");
                        Console.WriteLine();
                        Console.WriteLine("-dir     Directory to scan throught for files to convert. Default: D:\\TV");
                        Console.WriteLine("-extout  Extention of the converted file. Default: .mkv");
                        Console.WriteLine("-dest    Where to put converted file. Default: D:\\Finished Torrents\\");
                        return;
                }
            }

            string[] sAllFiles = { };
            foreach (string extention in FileFormats)
            {
                string[] sArrayTmp = Directory.GetFiles(MainDir, extention, SearchOption.AllDirectories);
                int sAllFilesLenght = sAllFiles.Length;
                Array.Resize<string>(ref sAllFiles, sAllFilesLenght + sArrayTmp.Length);
                Array.Copy(sArrayTmp, 0, sAllFiles, sAllFilesLenght, sArrayTmp.Length);
            }

            var ffprobe = new Process();
            ffprobe.StartInfo = new ProcessStartInfo("ffprobe.exe");
            ffprobe.StartInfo.RedirectStandardOutput = true;
            ffprobe.StartInfo.RedirectStandardError = true;
            ffprobe.StartInfo.UseShellExecute = false;

            foreach (string file in sAllFiles)
            {
                string codec = "", comment = "",error;
                int bitrate = 0;
                try
                {
                    ffprobe.StartInfo.Arguments = "-v error -select_streams v:0 -show_entries stream=codec_name -of default=noprint_wrappers=1:nokey=1 \"" + file + "\"";
                    ffprobe.Start();

                    codec = ffprobe.StandardOutput.ReadToEnd();
                    error = ffprobe.StandardError.ReadToEnd();

                    ffprobe.WaitForExit();

                    ffprobe.StartInfo.Arguments = "-v error -show_entries format=bit_rate -of default=noprint_wrappers=1:nokey=1 \"" + file + "\"";
                    ffprobe.Start();

                    bitrate = Convert.ToInt32(ffprobe.StandardOutput.ReadToEnd());
                    error = ffprobe.StandardError.ReadToEnd();

                    ffprobe.WaitForExit();

                    ffprobe.StartInfo.Arguments = "-v error -show_entries format_tags=comment -of default=noprint_wrappers=1:nokey=1 \"" + file + "\"";
                    ffprobe.Start();

                    comment = ffprobe.StandardOutput.ReadToEnd();
                    error = ffprobe.StandardError.ReadToEnd();

                    ffprobe.WaitForExit();
                }
                catch
                {
                    Console.WriteLine("Unable to start ffprobe. Press any key to close.");
                    Console.ReadLine();
                    break;
                }

                FileInfo fileInfo = new FileInfo(file);

                if (((codec != "hevc\r\n") || (bitrate > 2500000)) && (comment != "Encoded Kutayz\r\n"))
                {
                    var ffmpeg = new Process();
                    ffmpeg.StartInfo = new ProcessStartInfo("ffmpeg.exe");
                    ffmpeg.StartInfo.UseShellExecute = false;
                    string newfile = file.Split(new Char[] { '.' })[0] + "+++encoding+++" + FinalExtention;

                    ffmpeg.StartInfo.Arguments = "-i \"" + file + "\" -c:v libx265 -preset slower -crf 21 -c:a copy -c:s copy -metadata comment=\"Encoded Kutayz\" " + "\"" + newfile + "\"";

                    try
                    {
                        ffmpeg.Start();
                        ffmpeg.WaitForExit();
                    }
                    catch
                    {
                        Console.WriteLine("Unable to start ffmpeg. Press any key to close.");
                        Console.ReadLine();
                        break;
                    }

                    File.Delete(file);
                    File.Move(newfile, file.Split(new Char[] { '.' })[0] + FinalExtention);

                    if(Freeze)
                    {
                        Console.ReadLine();
                    }
                    
                    break;
                }
            }
        }
    }
}
