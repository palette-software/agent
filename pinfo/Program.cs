using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using fastJSON;
using System.Reflection;

    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length < 1)
            {
                //Example: pinfo du (returns disk usage)
                Console.WriteLine("Usage: pinfo.exe /du");
                return -1;
            }
            else if (args[0] == @"/du")
            {
                Dictionary<string, object> data = GetDriveInfo();

                string json = fastJSON.JSON.Instance.ToJSON(data);

                Console.Out.WriteLine(json);

                return 0;
            }

            else
            {
                Console.WriteLine("Usage: pinfo.exe /du");
                return -1;
            }
        }

        private static Dictionary<string, object> GetDriveInfo()
        {
            Dictionary<string, object> data = new Dictionary<string, object>();

            //Get the current drive
            string driveLetter = Path.GetPathRoot(Environment.CurrentDirectory);

            //return dictionary for primary drive only
            DriveInfo di = new DriveInfo(driveLetter);

            data["Name"] = di.Name;
            data["Drive type"] = di.DriveType;
            data["Volume label"] = di.VolumeLabel;
            data["Drive format"] = di.DriveFormat;
            data["Available space to current user"] = di.AvailableFreeSpace;
            data["Total available space"] = di.TotalFreeSpace;
            data["Total size of drive"] = di.TotalSize;

            return data;

            /* IF WE NEED TO GET SPACE FOR ALL DRIVES, USE THIS 
            DriveInfo[] allDrives = DriveInfo.GetDrives();

            foreach (DriveInfo d in allDrives)
            {
                Console.WriteLine("Drive {0}", d.Name);
                Console.WriteLine("  File type: {0}", d.DriveType);
                if (d.IsReady == true)
                {
                    Console.WriteLine("  Volume label: {0}", d.VolumeLabel);
                    Console.WriteLine("  File system: {0}", d.DriveFormat);
                    Console.WriteLine(
                        "  Available space to current user:{0, 15} bytes",
                        d.AvailableFreeSpace);

                    Console.WriteLine(
                        "  Total available space:          {0, 15} bytes",
                        d.TotalFreeSpace);

                    Console.WriteLine(
                        "  Total size of drive:            {0, 15} bytes ",
                        d.TotalSize);
                } 
            }
            */
        }
    }