using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq.Expressions;


internal class Program
{
    static string allFilesParentPath = "";
    static string manfiestFilePath = "";
    static JObject root;
    static string destinationPath = "";
    static Dictionary<string, KeyValuePair<string, JToken>> foldersDict = new Dictionary<string, KeyValuePair<string, JToken>>();//Dictionary<id, KeyValuePair<path, JToken>>


    static void Main(string[] args)
    {
        Console.WriteLine("-----Started-----");

        allFilesParentPath = getThePathOfTheExe();
        //allFilesParentPath = "/Users/nexgeninnovators/Downloads/STARTAR BNCC/";

        GetUserConfirmation("This is the parent folder of the extracted files: " + allFilesParentPath + " ?", 
        ifYes: ()=>{
                changeManfiestFileToTxt(allFilesParentPath + "manifest");
                Console.WriteLine(manfiestFilePath);

                createDestinationPath(allFilesParentPath);

                if(!string.IsNullOrEmpty(destinationPath))
                {
                    if(!string.IsNullOrEmpty(manfiestFilePath))
                    {
                        root = readTextFile(manfiestFilePath); 
                    
                        createFolders();
        
                        moveFiles();
                    }
                }
                
        }, 
        ifNo: ()=>{
                Console.WriteLine("Enter the path of the parent folder of the extracted files");
                allFilesParentPath = Console.ReadLine();

        });


        Console.WriteLine("----Finished----");
        Console.WriteLine("Press any key to continue");

        Console.ReadLine();
    }
    static string getThePathOfTheExe(){
        return AppContext.BaseDirectory;
    }

    static void GetUserConfirmation(string message, Action? ifYes = null, Action? ifNo =null){
        Console.WriteLine(message);
        Console.WriteLine("Type Y/N to continue");
        string? userInput = Console.ReadLine();
        if(userInput is not null)
        {
            if (userInput.Equals("Y", StringComparison.OrdinalIgnoreCase))
            {
                if(ifYes is not null)
                    ifYes();          
                                
            }  
            else if (userInput.Equals("N", StringComparison.OrdinalIgnoreCase))
            {
                if(ifNo is not null)
                    ifNo();
                GetUserConfirmation(message, ifYes, ifNo);    

            } 
            else {
                GetUserConfirmation(message, ifYes, ifNo);
            }         
        }
        else {
            GetUserConfirmation(message, ifYes, ifNo);
        }

    }

     static void changeManfiestFileToTxt(string filePath){
        //TODO: if txt already exists proceed with that
        Console.WriteLine("Changing the manfiest file to txt");
        Console.WriteLine(filePath);
        if(File.Exists(filePath)){
            Directory.Move(filePath, filePath + ".txt");
            manfiestFilePath = filePath + ".txt";
            Console.WriteLine("Conversion finished");
        }    
        else{
            Console.WriteLine("Couldn't find manfiest file. Are you sure you have given the right path? Please reopen the console application or try re-extracting the cclibs and try again");
        }

    }

    static void createDestinationPath(string exePath){

        Console.WriteLine("Creating Destination Path");
        // Specify the name of the new folder
        string newFolderName = "CreativeCloudOrganisedFiles";
        // Combine the parent directory path and the new folder name
        string parentDirectory = Directory.GetParent(exePath).Parent.FullName;
        string newFolderPath = Path.Combine(parentDirectory, newFolderName);
        // Check if the new folder already exists, and create it if not
        if (!Directory.Exists(newFolderPath))
        {
            Directory.CreateDirectory(newFolderPath);
            Console.WriteLine("The destination folder is: " + newFolderPath);
            destinationPath = newFolderPath;

        }
        else
        {
            destinationPath = null;
            Console.WriteLine("The destination folder already exits, delete the folder and please re-extract the cclibs for a fresh start");
        }      
    }

    static JObject readTextFile(string filePath)
    {
        Console.WriteLine("Reading the manfiest file");
        // Read the entire content of the file
        string jsonContent = File.ReadAllText(filePath);

        // Parse the JSON content using JObject
        return JObject.Parse(jsonContent);
    }

    static void createFolders()
    {
        //token that contains the folder data
        JToken folderDataJT = root["children"][1]["children"];
        Console.WriteLine("Creating Folders at: " + destinationPath);

        for (int i = 0; i < folderDataJT.Count(); i++)
        {
            if (folderDataJT[i]["library#parentId"] == null)
            {

                //then its master parent folder
                string id = folderDataJT[i]["id"].ToString();
                string pathWithId = destinationPath + id;
                string fullPath = Path.Combine(destinationPath, folderDataJT[i]["name"].ToString()); 

                if (Directory.Exists(pathWithId))
                {
                    if (Directory.Exists(fullPath))
                    {
                        MergeDirectories(pathWithId, fullPath);
                        
                    }
                    else
                    {
                        Directory.Move(pathWithId, fullPath);
                        
                    }
                    //change the path of previous files
                   
                    for (int j = 0; j < foldersDict.Count; j++)
                    {
                        
                        if (foldersDict.ElementAt(j).Value.Value["library#parentId"] != null && foldersDict.ElementAt(j).Value.Value["library#parentId"].ToString() == id)
                        {

                            string newFolderPath = fullPath + "/" + foldersDict.ElementAt(j).Value.Value["name"];
                            foldersDict[foldersDict.ElementAt(j).Key] = new KeyValuePair<string, JToken>(newFolderPath, foldersDict.ElementAt(j).Value.Value);
                        }
                    }

                }

                else
                {
                    Directory.CreateDirectory(fullPath);
                }


                foldersDict.Add(folderDataJT[i]["id"].ToString(), new KeyValuePair<string, JToken>(fullPath, folderDataJT[i]));  

            }
            else
            {
                //then its child folder

                //serach if parent id is in the dictionay
                string parentId = folderDataJT[i]["library#parentId"].ToString();
                string fullPath = "";

                if (foldersDict.ContainsKey(parentId))
                {
                    fullPath = foldersDict[parentId].Key + "/" + folderDataJT[i]["name"];
                    Directory.CreateDirectory(fullPath);
                }
                else
                {
                    fullPath = destinationPath + parentId + "/" + folderDataJT[i]["name"];
                    Directory.CreateDirectory(fullPath);
                }
                foldersDict.Add(folderDataJT[i]["id"].ToString(), new KeyValuePair<string, JToken>(fullPath, folderDataJT[i]));
                
            }

            
           
        }


       
        static void MergeDirectories(string pathWithID, string fullPath)
        {

            DirectoryInfo dirInfo = new DirectoryInfo(pathWithID);

            List<string> allSubFiles = Directory
                               .GetFiles(pathWithID, "*.*", SearchOption.AllDirectories).ToList();

            foreach (string file in allSubFiles)
            {
                FileInfo mFile = new FileInfo(file);
                // to remove name collisions
                if (new FileInfo(dirInfo + "/" + mFile.Name).Exists == false)
                {
                    mFile.MoveTo(dirInfo + "/" + mFile.Name);
                }
            }
        }
       Console.WriteLine("Created all the folder hierarchy");
    }
   
    static void moveFiles(){
         //token that contains the files data
        JToken filesDataJT = root["children"][0]["children"];
        Console.WriteLine("Moving files");


        for (int i = 0; i < filesDataJT.Count(); i++)
        {
            //if filesDataJt[i][id]
            //if id directory exists
            //then move it to this folder
            string existingFilePath = allFilesParentPath + filesDataJT[i]["id"];
            if (Directory.Exists(existingFilePath))
            {
               //if the file belongs to a folder move the the folder
               //else move it to the destination parent
                if(filesDataJT[i]["library#groups"]!=null){

                    //fetch parent folder id
                    JProperty prop = (JProperty)filesDataJT[i]["library#groups"].Children().First();
                    string parrentId = ExtractIdFromPropertyName(prop.Name);

                    //if we did create the folder for this parent id, then move it to the that folder
                    if (foldersDict.ContainsKey(parrentId))
                    {
                        string newFilePath = foldersDict[parrentId].Key + "/" + filesDataJT[i]["name"];
                        //if it is NOT a duplicate file with a name that was moved earlier then move to the folder
                        //else move it to a folder with same destination folder and add the index numner to it
                        if (!Directory.Exists(newFilePath))
                        {
                            Directory.Move(existingFilePath, newFilePath);
                            RenameInnerFilesOfFolders(newFilePath, filesDataJT[i]["name"].ToString());
                            
                            
                        }
                        else
                        {
                            Directory.Move(existingFilePath, newFilePath + "copy" + i );
                            RenameInnerFilesOfFolders(newFilePath, filesDataJT[i]["name"] + "copy" + i);
                        }
                    }
                    else
                    {
                        Console.WriteLine("folder was not created for the id: " + parrentId);
                    }   
                    
                }
                else
                {
                    Console.WriteLine(destinationPath + "/" + filesDataJT[i]["name"]);
                    Directory.Move(existingFilePath, destinationPath + "/" + filesDataJT[i]["name"]);
                    
                }
                
                
            }
            else{
                Console.WriteLine("Coudn't find the directory for id: " + filesDataJT[i]["id"]);
            }
        }
        static string ExtractIdFromPropertyName(string propertyName)
        {
            int hashIndex = propertyName.IndexOf('#');
            if (hashIndex != -1 && hashIndex < propertyName.Length - 1)
            {
                return propertyName.Substring(hashIndex + 1);
            }
            return null;
        }
        static void RenameInnerFilesOfFolders(string folderPath, string newFileName)
        {

            string[] files = Directory.GetFiles(folderPath);
            int index = 0;
            foreach (string filePath in files)
            {   
            
                string fileName = Path.GetFileNameWithoutExtension(filePath);
                string fileExtension = Path.GetExtension(filePath);
                string finalName = $"{newFileName}{fileExtension}";

                string newPath = Path.Combine(folderPath, finalName);
                if(!File.Exists(newPath))
                {
                    File.Move(filePath, newPath);

                }
                else
                {
                    string name = Path.GetFileNameWithoutExtension(newPath);
                    string extension = Path.GetExtension(newPath);
                    string _finalName = $"{name}{index}{extension}";
                    string _newPath = Path.Combine(folderPath, _finalName);
                    File.Move(filePath, _newPath);
                    index++;

                }

            
            }
        }
        
        Console.WriteLine("FINISHED MOVING ALL THE FILES AT PATH: " +  destinationPath);
    }
}

    



