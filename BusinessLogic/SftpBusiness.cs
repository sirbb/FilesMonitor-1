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
            var savedMonitoredFileInfoList = new List<FileMonitor>();

            using var sftpClient = new SftpClient(_pathSetting.SftpCredentials.Sftp, _pathSetting.SftpCredentials.Port, _pathSetting.SftpCredentials.Username, _pathSetting.SftpCredentials.Password);
            
            try
            {
                sftpClient.Connect();

               string serverFolder = sftpClient.WorkingDirectory;
               sftpClient.ChangeDirectory(working_directory);

                if (sourceFiles.Any())
                {
                    foreach(var file in sourceFiles)
                    {
                        await using(Stream stream = File.OpenRead(file))
                        {
                            sftpClient.UploadFile(stream, serverFolder + Path.GetFileName(file), Console.Write);
                        }

                        //save copied files info into DB
                        var monitoredFile = new FileMonitor
                        {
                            TimeCopied = DateTime.UtcNow
                        };
                        var bytes = await System.IO.File.ReadAllBytesAsync(file);
                        monitoredFile.FileCopied = bytes;
                        
                        //add to list
                        savedMonitoredFileInfoList.Add(monitoredFile);

                        //save monitored info
                        await SaveCopiedInfo(savedMonitoredFileInfoList);
                    }
                }
                
                sftpClient.Disconnect();
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
