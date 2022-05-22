using System.Collections.Generic;
using UnityEngine;
using UnityGoogleDrive;
//using System.Diagnostics;
using CSharpTree;

public class TestFilesList : AdaptiveWindowGUI
{
    [Range(1, 1000)]
    public int ResultsPerPage = 100;

    private GoogleDriveFiles.ListRequest request;
    private Dictionary<string, string> results;
    private string query = string.Empty;
    private Vector2 scrollPos;

    private void Start ()
    {
        ListFiles();
    }

    protected override void OnWindowGUI (int windowId)
    {
        if (request.IsRunning)
        {
            GUILayout.Label($"Loading: {request.Progress:P2}");
        }
        else if (results != null)
        {
            scrollPos = GUILayout.BeginScrollView(scrollPos);
            foreach (var result in results)
            {
                GUILayout.Label(result.Value);
                GUILayout.BeginHorizontal();
                GUILayout.Label("ID:", GUILayout.Width(20));
                GUILayout.TextField(result.Key);
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
        }

        GUILayout.BeginHorizontal();
        GUILayout.Label("File name:", GUILayout.Width(70));
        query = GUILayout.TextField(query);
        if (GUILayout.Button("Search", GUILayout.Width(100))) ListFiles();
        if (NextPageExists() && GUILayout.Button(">>", GUILayout.Width(50)))
            ListFiles(request.ResponseData.NextPageToken);
        GUILayout.EndHorizontal();
    }

    private void ListFiles (string nextPageToken = null)
    {
        request = GoogleDriveFiles.List();
        // description, iconLink, createdTime, sharedWithMeTime, sharingUser, owners, ownedByMe
        request.Fields = new List<string> { "nextPageToken, files(mimeType, id, name, kind, parents, hasThumbnail)" };
        request.PageSize = ResultsPerPage;
        if (!string.IsNullOrEmpty(query))
            request.Q = string.Format("name contains '{0}'", query);
        if (!string.IsNullOrEmpty(nextPageToken))
            request.PageToken = nextPageToken;
        request.Send().OnDone += BuildResults;
    }

    private void BuildResults (UnityGoogleDrive.Data.FileList fileList)
    {
        results = new Dictionary<string, string>();

        var fi = string.Format(" ");

        int count = 0;
        foreach (var file in fileList.Files)
        {
            //var fileInfo = string.Format("NAME: {0} \tTYPE: ", file.Name);
            fi += string.Format("\nNAME: {0} \tTYPE: ", file.Name);

            if(file.MimeType.Contains("folder"))
            {
                fi += string.Format("folder {0}", file.Id);

                Debug.LogFormat("MIME: {0} ID: {1} NAME: {2} KIND: {3} PARENT: {4} THUMB: {5}", file.MimeType, file.Id, file.Name, file.Kind, file.Parents[0], file.HasThumbnail);
                //fileInfo += string.Format("folder");
            }  
          
           /* else if(file.MimeType.Contains("image"))
            {
                 fileInfo += "image";
            }
        */
            else if(file.MimeType.Contains("jpeg") || file.MimeType.Contains("png"))
            {
                fi += "image";
            }

            else if(file.MimeType.Contains("video"))
            {
                fi += "video";
            }

            else if(file.MimeType.Contains("pdf"))
            {
                fi += "PDF";
            }

            else 
            {
                fi += file.MimeType;
            }

            fi += string.Format("\tPARENT: {0}", file.Parents[0]);

           count++;
        }

        Debug.LogFormat("COUNTED {0} items", count);

        Debug.Log(fi);



        Dictionary<string, UnityGoogleDrive.Data.File> id2File = new Dictionary<string, UnityGoogleDrive.Data.File>();
        Dictionary<string, string> id2Parent = new Dictionary<string, string>();



        foreach(var file in fileList.Files)
        {
            id2File.Add(file.Id, file);
            id2Parent.Add(file.Id, file.Parents[0]);
        }
        
        UnityGoogleDrive.Data.File root = FindRoot(id2Parent);
        UnityGoogleDrive.Data.File dir = fileList.Files[0];

        foreach (var file in fileList.Files)
        {
            if(file.MimeType.Contains("folder"))
                dir = file;
                break;
        }

        // OnClickedIcon() //DoubleClicked
        if(selectedFile.MimeType.Contains("folder"))
        {
            List<string> filesInDirectory = GetFilesInDirectory(dir, id2Parent);

            DisplayFiles(filesInDirectory);
        }

        else
        {
            ShowItemInfo()
        }

        foreach(var _f in inDir)
        {
           
            Debug.LogFormat("In {0}: {1}",  dir.Name, id2File[_f].Name);
        }


        
        foreach (var file in fileList.Files)
        {
            var fileInfo = string.Format("Name: {0} mime {1} thumbnail {2}",// parents {3}",
          //  var fileInfo = string.Format("Name: {0} Size: {1:0.00}MB Created: {2:dd.MM.yyyy}",
                file.Name,
              //  file.Kind,
                file.MimeType,
                file.HasThumbnail);
             //   file.Parents[0]);
            results.Add(file.Id, fileInfo);
        }        
    }
    
    private List<string> GetFilesInDirectory(UnityGoogleDrive.Data.File directory, Dictionary<string, string> files)
    {
        List<string> inDirectory = new  List<string>();

        foreach(var item in files)
        {
            // if parent is value then add to list of current
            if(item.Value == directory.Id)
            {
                inDirectory.Add(item.Key);
            }
        } 

        return inDirectory;
    }

    private UnityGoogleDrive.Data.File FindRoot(Dictionary<string, string> dict)
    {
        UnityGoogleDrive.Data.File root = new UnityGoogleDrive.Data.File();

        root.MimeType   = "application/vnd.google-apps.folder";
        root.Parents    = null;
      
        foreach(var item in dict)
        {
            if (!dict.ContainsKey(item.Value))
            {
                root.Id = item.Value;
                break;
            }
        }
    
        return root;
    }


    private bool NextPageExists ()
    {
        return request != null && 
            request.ResponseData != null && 
            !string.IsNullOrEmpty(request.ResponseData.NextPageToken);
    }
}
