using System.Collections.Generic;
using UnityEngine;
using UnityGoogleDrive;
//using System.Diagnostics;

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

        foreach (var file in fileList.Files)
        {
            var fileInfo = string.Format("NAME: {0} \tTYPE: ", file.Name);

            if(file.MimeType.Contains("folder"))
            {
                fileInfo += string.Format("folder {0}", file.Id);
            }  
        
          /*  else if(file.MimeType.Contains("jpeg") || file.MimeType.Contains("png"))
            {
                fileInfo += "image";
            }
          */

            else if(file.MimeType.Contains("video"))
            {
                fileInfo += "video";
            }


            else
            {
                Debug.LogFormat("NONE OF THE ABOVE 0} {1}", file.Name, file.MimeType);
            }

            Debug.Log(fileInfo);
            Debug.LogFormat("PARENTS {0} {1}", file.Parents.Count, file.Parents[0]);

            results.Add(file.Id, fileInfo);
        }
        
        /*
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
        */
        
    }

    private bool NextPageExists ()
    {
        return request != null && 
            request.ResponseData != null && 
            !string.IsNullOrEmpty(request.ResponseData.NextPageToken);
    }
}
