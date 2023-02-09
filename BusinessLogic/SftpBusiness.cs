using BusinessLogic.Interfaces;
using Entities;
using Entities.Entities;
using Microsoft.Extensions.Options;
using Renci.SshNet;
using Repository.Interfaces;

namespace BusinessLogic
{
    public class SftpBusiness : ISftpBusiness
    {
        private readonly PathSetting _pathSetting;
        private readonly ISftpRepository _sftpRepository;
        public SftpBusiness(IOptions<PathSetting> pathSetting, ISftpRepository sftpRepository)
        {

            _pathSetting = pathSetting.Value;
            _sftpRepository = sftpRepository;
        }

        /// <summary>
        /// Monitor folder for files
        /// </summary>
        /// <returns>The list of files</returns>
        public async Task<List<string>> MonitorFolder()
        {
            var sourceFiles = new List<string>();
            var savedMonitoredFileInfoList = new List<FileMonitor>();
  
            try
            {
                using var sftpClient = new SftpClient(_pathSetting.SftpCredentials.Sftp, _pathSetting.SftpCredentials.Port, _pathSetting.SftpCredentials.Username, _pathSetting.SftpCredentials.Password);
                //using (SftpClient sftpClient = new Renci.SshNet.SftpClient("74.205.82.143", 22, "richardA", "Onvida!sThePlace2B"))
                sftpClient.Connect();

                //get source folder
                var sourceFolder = _pathSetting.SourceFolder;
                const string fileExtension = ".zip";

                if (Directory.Exists(sourceFolder))
                {
                    var fileEntries = Directory.GetFiles(sourceFolder, "*" + fileExtension);
                    foreach(var file in fileEntries)
                    {
                        sourceFiles.Add(file);

                        //create db object to save each instance monitored
                        var monitoredFile = new FileMonitor
                        {
                            TimeMonitored = DateTime.UtcNow
                        };
                        var bytes = await File.ReadAllBytesAsync(file);
                        monitoredFile.FileCopied = bytes;

                        //add to list
                        savedMonitoredFileInfoList.Add(monitoredFile);

                        //save monitored info
                        await SaveMonitoringInfo(savedMonitoredFileInfoList);
                    }
                }
                
                sftpClient.Disconnect();
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return sourceFiles;
        }

       /// <summary>
        /// Monitor folder for files
        /// </summary>
        /// <returns>The list of files</returns>
        public async Task<List<string>> UploadFilesToServer()
        {
            var sourceFiles = await MonitorFolder();
            var savedMonitredFileInfoList = new List<FileMonitor>();
            //string[] fileEntries = new string[]; 
            try
            {
                //host, port, username, paswword
               using var sftpClient = new SftpClient(_pathSetting.SftpCredentials.Sftp, _pathSetting.SftpCredentials.Port, _pathSetting.SftpCredentials.Username, _pathSetting.SftpCredentials.Password);
               //using (SftpClient sftpClient = new Renci.SshNet.SftpClient("74.205.82.143", 22, "richardA", "Onvida!sThePlace2B"))
                {
                    sftpClient.Connect();
                    //get directories
                    string working_directory = sftpClient.WorkingDirectory;
                    var newFolder = "Uploaded Zip Files";
                    //string current = "";


                    //if (working_directory[0] == '/')
                    //{
                    //    var directories = sftpClient.ListDirectory(working_directory);
                    //}
                   // var directories = sftpClient.ListDirectory(working_directory)
               string serverFolder = sftpClient.WorkingDirectory;
               sftpClient.ChangeDirectory(working_directory);

                    //get source folder
                    //var serverFolder = _patheSetting.DestinationFolder;

                    
                    sftpClient.ChangeDirectory(working_directory);

                    if (sourceFiles != null && sourceFiles.Any())
                    {
                        foreach(var file in sourceFiles)
                        {
                            using (Stream stream = File.OpenRead(file))
                            {
                                sftpClient.UploadFile(stream, working_directory + Path.GetFileName(file), x =>
                                {
                                    Console.Write(x);
                                });
                            }

                            //using (var stream = new FileStream(file, FileMode.Open))
                            //{
                            //    var content = Path.GetFileName(file);   
                            //    sftpClient.UploadFile(stream, content, x =>
                            //    {
                            //        Console.Write(x);
                            //    });
                            //}

                            //save copied files info into DB
                            var monitoredFile = new FileMonitor();
                            monitoredFile.TimeCopied = DateTime.UtcNow;
                            byte[] bytes = System.IO.File.ReadAllBytes(file);
                            monitoredFile.FileCopied = bytes;

                            //add to list
                            savedMonitredFileInfoList.Add(monitoredFile);

                            //save monitored info
                            await SaveCopiedInfo(savedMonitredFileInfoList);
                        }
                    }


                    sftpClient.Disconnect();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return sourceFiles;
        }

        /// <summary>
        /// Save folder  monitoring info
        /// </summary>
        /// <param name="fileMonitorList">The monitored files info</param>
        /// <returns></returns>
        private async Task SaveMonitoringInfo(IEnumerable<FileMonitor> fileMonitorList)
        {
            try
            {
                await _sftpRepository.SaveList(fileMonitorList);

            }
            catch (Exception ex)
            {
                // ignored
            }
        }

        /// <summary>
        /// Saves files copied info
        /// </summary>
        /// <param name="fileMonitorList">The monitored files info</param>
        /// <returns></returns>
        private async Task SaveCopiedInfo(IEnumerable<FileMonitor> fileMonitorList)
        {
            try
            {  
                //save copied files information
                await _sftpRepository.SaveList(fileMonitorList);

                //delete copied files after a successful save
                await DeleteFiles(_pathSetting.DestinationFolder);
                
            }
            catch (Exception ex)
            {
                // ignored
            }
        }

    
        /// <summary>
        /// 
        /// </summary>
        /// <param name="dirPath"></param>
        /// <returns></returns>
        private static Task<bool> DeleteFiles(string dirPath)
        {
            var di = new DirectoryInfo(dirPath);
            var arrFi = di.GetFiles("*.*");
            
            try
            {
                foreach (var fi in arrFi)
                {
                    File.Delete(di + "\\" + fi.Name);
                }
                return Task.FromResult(true);
            }
            catch
            {
                return Task.FromResult(false);
            }
        }
    }
}
