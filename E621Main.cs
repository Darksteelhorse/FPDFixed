using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Xml;
using System.Xml.Linq;
using System.IO;
using System.Threading;

namespace WebRequest1
{


    partial class E621Main
    {
        
        
        const string e621BaseQuery = "https://e621.net/post/index.xml?tags=";
        const string beforeIDBase = "&before_id=";
        const string searchLimit = "&limit=320";
        const string programID = "DesktopProject/Alpha(kaitoukitsune)";
        
       static Semaphore sem = new Semaphore(10, 11);
        static void Main(string[] args)
        {


            string[] searchTags = new string[6];
            int pageCount = 0;
            bool continueSearch = false;                   
            List<XElement> pages = new List<XElement>();
            string directory = Properties.Settings.Default.directory;
             

            
            bool keepMenu = false;
            do
            {
                int menuSelection = DisplayMenu();
                switch (menuSelection)
                {
                    case 1:
                        for (int i = 0; i < 6; i++)
                        {
                            Console.Write("Enter a tag: ");
                            searchTags[i] = Console.ReadLine();
                            if (searchTags[i] == "")
                            {
                                searchTags[i] = null;
                                break;
                            }
                        }

                        keepMenu = repeatMenu();
                        break;

                    case 2:
                        Console.WriteLine("Enter Username:");
                        string userName = Console.ReadLine();
                        Console.WriteLine("Enter Password:");
                        string userPass = Console.ReadLine();
                        //TODO: secure these strings, pass login to E621
                        keepMenu = repeatMenu();
                        break;

                    case 3:

                        Console.WriteLine("Enter tags to remove from queries. See (enter website here) for formatting instructions");
                        List<string> blackListags = new List<string>();
                        string tag;
                        bool validTag = true;
                        do
                        {
                            tag = Console.ReadLine();
                            if (tag != "")
                            {
                                blackListags.Add(Console.ReadLine());
                            }
                            else
                            {
                                validTag = false;
                            }

                        } while (validTag);
                        keepMenu = repeatMenu();
                        break;
                    case 4:
                        Console.WriteLine("The download path is: " + directory);
                       
                        keepMenu = false;
                        break;
                    case 5:
                        directory = loadUserValues();
                        keepMenu = true;
                        break;
                    case 6:
                        editUserValues();
                        keepMenu = true;
                        break;


                    default:
                        Console.WriteLine("Invalid number! Try again.");
                        keepMenu = true;
                        
                        break;
                }
            } while (keepMenu);










            Console.WriteLine(E621Builder(searchTags));

            // Create a new request to the mentioned URL.	
            HttpWebRequest myWebRequest = (HttpWebRequest)WebRequest.Create(E621Builder(searchTags));
            

            myWebRequest.Method = "POST";

            myWebRequest.UserAgent = programID;

            // Assign the response object of 'WebRequest' to a 'WebResponse' variable.
            WebResponse myWebResponse = myWebRequest.GetResponse();

            Console.WriteLine(((HttpWebResponse)myWebResponse).StatusDescription);


            XDocument xml = XDocument.Load(myWebResponse.GetResponseStream());
            // Release the resources of response object.
            myWebResponse.Close();

            

            List<XElement> posts = new List<XElement>();

            foreach (var item in xml.Descendants("post"))
            {
                //Console.WriteLine( item.Element("tags").Value);
                posts.Add(item);
            }
            
            Console.WriteLine(" There are " + posts.Count + " items in the List!");


            
            do
            {
                if (posts.Count() >= 320)
                {
                    pageCount++;
                    continueSearch = true;
                    myWebRequest = (HttpWebRequest)nextE621Request(searchTags, posts.Last().Element("id").Value.ToString());

                    myWebRequest.Method = "POST";

                    myWebRequest.UserAgent = programID;

                    // Assign the response object of 'WebRequest' to a 'WebResponse' variable.
                    myWebResponse = myWebRequest.GetResponse();

                    Console.WriteLine(((HttpWebResponse)myWebResponse).StatusDescription);


                    xml = XDocument.Load(myWebResponse.GetResponseStream());
                    // Release the resources of response object.
                    myWebResponse.Close();

                    

                    foreach (var item in xml.Descendants("post"))
                    {
                                            


                        posts.Add(item);
                    }

                    Console.WriteLine(" There are " + posts.Count + " in the List!");
                    if (xml.Descendants("post").Count()<320)
                    {
                        continueSearch = false;

                    }
                   

                }
                else
                {
                    continueSearch = false;
                }
            } while (continueSearch);

            // send the files to directory
            // saveFile(posts, directory);
            
            
             
            for (int i = 0; i < posts.Count; i++)
            {

                sem.WaitOne();
                Thread[] threads = new Thread[posts.Count];
                Image dlFile = new FileObject();

                string fileID;
                string fileURL;
                string artist;
                string fileType;

                StringBuilder sb = new StringBuilder();
                

                fileID = posts[i].Element("id").Value.ToString();
                fileURL = posts[i].Element("file_url").Value.ToString();
                artist = posts[i].Element("artist").Value.ToString();
                fileType = posts[i].Element("file_ext").Value.ToString();
                sb.AppendFormat("{0}{1}-{2}.{3}", directory, artist, fileID, fileType);
                dlFile.url = fileURL;
                dlFile.dlDirectory = sb.ToString();
                



                Console.WriteLine("Threads starting.....");
                
                threads[i] = new Thread(saveFile);
                threads[i].Name = "thread_" + i;
                threads[i].Start(dlFile);
                 
                       
                //saveFile(posts[i], directory);
               
                       
                    
                    
                
                    


            }
            
             
        }

        
    }
}
