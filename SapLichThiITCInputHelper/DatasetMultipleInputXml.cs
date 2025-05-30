﻿namespace SapLichThiITCInputHelper
{
    public class DatasetMultipleInputXml
    {
        public string I_folderPath = string.Empty;

        public DatasetMultipleInputXml(string folderPath)
        {
            I_folderPath = folderPath;
        }

        public string[] GetAllFilesInFolderAndSubfolder(string folderPath)
        {
            return Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories).Where(x => x.EndsWith(".xml")).ToArray();
        }

        public DatasetMultipleInputXml Run()
        {
            var filePaths = GetAllFilesInFolderAndSubfolder(I_folderPath);
            foreach (var filePath in filePaths)
            {

            }

            return this;
        }

    }
}
