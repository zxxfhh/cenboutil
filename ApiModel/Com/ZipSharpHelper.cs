using ICSharpCode.SharpZipLib.Checksum;
using ICSharpCode.SharpZipLib.Zip;
using Org.BouncyCastle.Utilities.Zlib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ApiModel.Com
{
    public class ZipSharpHelper
    {
        /// <summary>  
        /// 所有文件缓存  
        /// </summary>  
        List<string> files = new List<string>();

        /// <summary>  
        /// 所有空目录缓存  
        /// </summary>  
        List<string> paths = new List<string>();

        /// <summary>  
        /// 压缩单个文件  
        /// </summary>  
        /// <param name="fileToZip">要压缩的文件</param>  
        /// <param name="zipedFile">压缩后的文件全名</param>  
        /// <param name="compressionLevel">压缩程度，范围0-9，数值越大，压缩程序越高</param>  
        /// <param name="blockSize">分块大小</param>  
        public void ZipFile(string fileToZip, string zipedFile, int compressionLevel, int blockSize)
        {
            if (!System.IO.File.Exists(fileToZip))//如果文件没有找到，则报错  
            {
                throw new FileNotFoundException("The specified file " + fileToZip + " could not be found. Zipping aborderd");
            }

            FileStream streamToZip = new FileStream(fileToZip, FileMode.Open, FileAccess.Read);
            FileStream zipFile = File.Create(zipedFile);
            ZipOutputStream zipStream = new ZipOutputStream(zipFile);
            ZipEntry zipEntry = new ZipEntry(fileToZip);
            zipStream.PutNextEntry(zipEntry);
            zipStream.SetLevel(compressionLevel);
            byte[] buffer = new byte[blockSize];
            int size = streamToZip.Read(buffer, 0, buffer.Length);
            zipStream.Write(buffer, 0, size);

            try
            {
                while (size < streamToZip.Length)
                {
                    int sizeRead = streamToZip.Read(buffer, 0, buffer.Length);
                    zipStream.Write(buffer, 0, sizeRead);
                    size += sizeRead;
                }
            }
            catch (Exception ex)
            {
                GC.Collect();
                throw ex;
            }

            zipStream.Finish();
            zipStream.Close();
            streamToZip.Close();
            GC.Collect();
        }

        /// <summary>  
        /// 压缩目录（包括子目录及所有文件）  
        /// </summary>  
        /// <param name="rootPath">要压缩的根目录</param>  
        /// <param name="destinationPath">保存路径</param>  
        /// <param name="compressLevel">压缩程度，范围0-9，数值越大，压缩程序越高</param>  
        public void ZipFileFromDirectory(string rootPath, string destinationPath, int compressLevel = 9)
        {
            GetAllDirectories(rootPath);

            Crc32 crc = new Crc32();

            // 创建一个文件流来写入 zip 文件
            using (FileStream fsOut = File.Create(destinationPath))
            {
                // 创建一个 ZipOutputStream 来进行压缩
                using (ZipOutputStream zipStream = new ZipOutputStream(fsOut))
                {
                    zipStream.SetLevel(compressLevel); // 压缩级别 0-9，0 不压缩，9 为最大压缩

                    foreach (string file in files)
                    {
                        FileStream fileStream = File.OpenRead(file);//打开压缩文件  
                        byte[] buffer = new byte[fileStream.Length];
                        fileStream.Read(buffer, 0, buffer.Length);
                        string entryName = file.Substring(rootPath.Length + 1); // 去掉根文件夹路径
                        entryName = ZipEntry.CleanName(entryName); // 清理名称

                        ZipEntry entry = new ZipEntry(entryName);
                        entry.DateTime = DateTime.Now;
                        entry.Size = fileStream.Length;

                        fileStream.Close();
                        crc.Reset();
                        crc.Update(buffer);
                        entry.Crc = crc.Value;
                        zipStream.PutNextEntry(entry);
                        zipStream.Write(buffer, 0, buffer.Length);
                    }

                    this.files.Clear();

                    foreach (string emptyPath in paths)
                    {
                        ZipEntry entry = new ZipEntry(emptyPath.Replace(rootPath, string.Empty) + "/");
                        zipStream.PutNextEntry(entry);
                    }
                    this.paths.Clear();

                    zipStream.Finish();
                    zipStream.Close();
                }
            }

            GC.Collect();
        }

        /// <summary>  
        /// 取得目录下所有文件及文件夹，分别存入files及paths  
        /// </summary>  
        /// <param name="rootPath">根目录</param>  
        private void GetAllDirectories(string rootPath)
        {
            string[] subPaths = Directory.GetDirectories(rootPath);//得到所有子目录  
            foreach (string path in subPaths)
            {
                GetAllDirectories(path);//对每一个字目录做与根目录相同的操作：即找到子目录并将当前目录的文件名存入List  
            }
            string[] files = Directory.GetFiles(rootPath);
            foreach (string file in files)
            {
                this.files.Add(file);//将当前目录中的所有文件全名存入文件List  
            }
            if (subPaths.Length == files.Length && files.Length == 0)//如果是空目录  
            {
                this.paths.Add(rootPath);//记录空目录  
            }
        }

        /// <summary>  
        /// 解压缩文件(压缩文件中含有子目录)  
        /// </summary>  
        /// <param name="zipfilepath">待解压缩的文件路径</param>  
        /// <param name="unzippath">解压缩到指定目录</param>  
        /// <returns>解压后的文件列表</returns>  
        public List<string> UnZip(string zipfilepath, string unzippath)
        {
            //解压出来的文件列表  
            List<string> unzipFiles = new List<string>();

            using (var zipInputStream = new ZipInputStream(File.OpenRead(zipfilepath)))
            {
                ZipEntry theEntry = zipInputStream.GetNextEntry();
                while (theEntry != null)
                {
                    string fileName = theEntry.Name;
                    // 指定文件名编码为GBK
                    fileName = Encoding.Default.GetString(Encoding.GetEncoding("GBK").GetBytes(fileName));
                    if (theEntry.IsDirectory)
                    {
                        string dicPath = Path.Combine(unzippath, fileName);
                        Directory.CreateDirectory(dicPath);
                    }
                    else
                    {
                        //string fileName = Path.GetFileName(theEntry.Name);
                        string fullPath = Path.Combine(unzippath, fileName);
                        if (!string.IsNullOrEmpty(fullPath))
                        {
                            if (theEntry.IsDirectory)
                            {
                                Directory.CreateDirectory(fullPath);
                            }
                            else
                            {
                                using (FileStream fileStream = File.Create(fullPath))
                                {
                                    int size = 2048;
                                    byte[] buffer = new byte[size];
                                    size = zipInputStream.Read(buffer, 0, buffer.Length);
                                    while (size > 0)
                                    {
                                        fileStream.Write(buffer, 0, size);
                                        size = zipInputStream.Read(buffer, 0, buffer.Length);
                                    }
                                }
                            }
                        }
                    }
                    theEntry = zipInputStream.GetNextEntry();
                }
            }
            GC.Collect();
            return unzipFiles;
        }

        public string GetZipFileExtention(string fileFullName)
        {
            int index = fileFullName.LastIndexOf(".");
            if (index <= 0)
            {
                throw new Exception("源包文件不是压缩文件");
            }

            //extension string
            string ext = fileFullName.Substring(index);

            if (ext == ".rar" || ext == ".zip")
            {
                return ext;
            }
            else
            {
                //The source package file is not a compress file
                throw new Exception("源包文件不是压缩文件");
            }
        }
    }
}