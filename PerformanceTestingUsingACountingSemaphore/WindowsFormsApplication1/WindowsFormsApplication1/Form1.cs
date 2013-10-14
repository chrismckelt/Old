using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Excel = Microsoft.Office.Interop.Excel;
using Office = Microsoft.Office.Core;
using Microsoft.Office.Tools;
using System.IO;
using System.Runtime.InteropServices;
using System.Data.SqlClient;
using System.Xml;
using System.Xml.XPath;



namespace WindowsFormsApplication1
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        public string FileName;
        public int ConcurrentThreads = 3;
        public volatile int AddtoDMSXtimes = 500;
        private DijkstraSemaphore semaphore; // counting semaphore
        private volatile Stack<Guid> guidStack = new Stack<Guid>();
        public volatile List<Guid> guidList = new List<Guid>();
        private ManualResetEvent resetEvent = new ManualResetEvent(false);
        
        private void Form1_Load(object sender, EventArgs e)
        {
            semaphore = new DijkstraSemaphore(0, ConcurrentThreads);
        }

       private void testButton_Click(object sender, EventArgs e)
       {
          
           Thread populateGuidStackThread = new Thread((ThreadStart)
                                                      delegate
                                                      {
                                                          for (int x = 0; x < AddtoDMSXtimes+1; x++)
                                                          {
                                                              guidStack.Push(Guid.NewGuid());
                                                          }

                                                          resetEvent.Set();
                                                      }
              );

           populateGuidStackThread.Start();


           Thread[] threads = new Thread[ConcurrentThreads];
           Stopwatch watch = new Stopwatch();
           watch.Start();

           int i;

           for (i = 0; i < ConcurrentThreads; i++)
           {
               threads[i] = new Thread(new ThreadStart(ExecuteTest));
               threads[i].Name = "Thread " + i.ToString();
           }

           watch.Stop();
           TimeSpan ts1 = watch.Elapsed;

           if (resetEvent.WaitOne())
           {
               watch.Start();

               foreach (Thread thread in threads)
               {
                   thread.Start();
               }

               watch.Stop();
           }

           TimeSpan ts2 = watch.Elapsed;

           semaphore.WaitForStarvation();


           Console.WriteLine("----------------- FINISHED -----------------------");
           Console.WriteLine("Time to build threads in seconds " + ts1.TotalSeconds);
           Console.WriteLine("Time for threads to finish in seconds " + ts2.TotalSeconds);
       }

       private void ExecuteTest()
       {
           Console.WriteLine(Thread.CurrentThread.Name);

           semaphore.Release();

           while  (AddtoDMSXtimes >= 0)
           {
               AddtoDMSXtimes--;
               
               guidList.Add(guidStack.Pop());
               for(int exampleCount=0;exampleCount<10-1;exampleCount++)
               {
                   Console.WriteLine(Thread.CurrentThread.Name + "   " + AddtoDMSXtimes.ToString() + " " + exampleCount.ToString());    
               }
           } 

           semaphore.Acquire();
       }
    }
}
