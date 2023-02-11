using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace App
{
    abstract class SocketServer
    {
        private TcpListener TCPServer = null;

        //the displayed TCP server name in error messages
        protected string TCPServerName;

        protected SocketServer(string ip, int port,string serverName)
        {
            //initialize the displayed TCP server name in error messages
            TCPServerName = serverName;

            //Listens for incoming connection attempts on the specified local IP address and port number.
            IPAddress localAddr = IPAddress.Parse(ip);
            TCPServer = new TcpListener(localAddr, port);
            TCPServer.Start();

            //画像処理アプリ(App1～6)からの接続依頼が来た場合、接続を完成させ、
            //1つ画像処理アプリに対して、1つのThreadを生成してその画像処理アプリとのコミュニケーションを実施する。
            StartListener();

            
        }


        //画像処理アプリ(App1～6)からの接続依頼が来た場合、接続を完成させ、
        //1つ画像処理アプリに対して、1つのThreadを生成してその画像処理アプリとのコミュニケーションを実施する。
        //画像処理アプリ(App1～6)全部の接続依頼が来た場合、
        //6つのThreadを生成してそれぞれの画像処理アプリとのコミュニケーションを実施する。
        public void StartListener()
        {
            //to contain log message
            string logMsg = "";


            try
            {
                //count the number of connected TCP clients
                int numOfTCPClient = 0;

                //keep checking if there is a TCP client that wants to connect to this TCP server
                while (true)
                {
                    //count the number of connected TCP clients
                    numOfTCPClient = numOfTCPClient + 1;


                    //Create a TCP socket connection with the client

                    //AcceptTcpClient is a blocking method that returns a TcpClient that you can use to send and receive data. 
                    //It will stop here until a TCP client's connection request comes.
                    TcpClient TCPClient = TCPServer.AcceptTcpClient();

                    logMsg = "Connected with " + numOfTCPClient + " App.";
                    Console.WriteLine(logMsg);

                    //Write msg to system log for debugging
                    /*
                    EventLogHandler.outputLog(logMsg);
                    */

                    //create a thread to send/receive messages with this TCP client(a App in App1~6)
                    Thread communicateWithClientThread = new Thread(() => CommunicateWithClient(TCPClient));
                    try
                    {
                        communicateWithClientThread.Start();
                    }
                    //Exception Handler for failing to create a thread. 
                    catch (OutOfMemoryException e)
                    {


                        //display this error message to inform the user
                        string errorMessage = "メモリ不足のせいで、" + GlobalConstants.TCPSocketServerName + "が" + "画像処理アプリからのメッセージを受け取るためのスレッドが実行できない。\n" + GlobalConstants.TCPSocketServerName + "が自動的に停止した。" + "\n\n停止ボタンを押して、\nを再開してください。\n\n再起動してもこのエラーが続ける場合、\nを停止して管理者に連絡してください。" + "\n\nエラーメッセージ：\n" + e.Message;
                        string errorMessageBoxTitle = " " + GlobalConstants.TCPSocketServerName + " スレッドが実行できないエラー";
                        string NASErrorTxtFileName = "画像処理アプリからのメッセージを受け取るためのスレッドが実行できない";
                        //show the error message box (if currently no error message box is shown) to inform the user and upload the error message to the アプリ_エラーメッセージ folder on NAS
                        ReportErrorMsg.showMsgBoxIfNotShown_UploadErrMsgToNAS(errorMessage, errorMessageBoxTitle, NASErrorTxtFileName);


                        //自動的にこのを停止させる。
                        return;
                    }

                }
            }
            catch (SocketException e)
            {
                string errorMessage = "TCP socket server can not set up connection with client.\n\nエラーメッセージ："+ e.Message;
                Console.WriteLine(errorMessage);

                //show a pop-up message to inform the operator to reboot the system
                ReportErrorMsg.showMsgBox_Anyway(GlobalConstants.TCPSocketServerName+ "はTCPでApp1 - 6と接続できない\n\n停止ボタンを押して、\nを再開してください。\n\n再起動してもこのエラーが続ける場合、\n停止ボタンを押してを停止して、\n管理者に連絡してください。", " " + GlobalConstants.TCPSocketServerName+ "_TCPでApp1 - 6と接続できないエラー");


                //output the error message 
                //to the DataManagementApp_エラーメッセージ folder in NAS
                ReportErrorMsg.outputErrorMsg("TCP socket server can not connect to client", errorMessage);

                //TCP severを停止させる.
                TCPServer.Stop();
                
            }
        }


        //本システムでは、が自ら送信することはない。
        //Send message to a specific App in App1~6 with TCP socket
        public static void sendMsgToSpecificTCPClient(NetworkStream specificClientTCPStream, string specificTCPClientName,string messageForClient)
        {
            string logMsg;

            //Retry  when error occurs
            for (int retryTimes = 1; retryTimes <= GlobalConstants.retryTimesLimit; retryTimes++)
            {
                try
                {
                    string serverMessage = messageForClient;
                    Byte[] reply = System.Text.Encoding.UTF8.GetBytes(serverMessage);
                    //Send the response message back to the App whose message has been received.
                    specificClientTCPStream.Write(reply, 0, reply.Length);

                    //display the current socket server thread that handles this connection.
                    //Console.WriteLine("{1}: Sent: {0}", str, Thread.CurrentThread.ManagedThreadId);
                    logMsg = "TCP server sends:" + serverMessage + " to " + specificTCPClientName;
                    Console.WriteLine(logMsg);

                    //Write msg to system log for debugging
                    /*
                    EventLogHandler.outputLog(logMsg);
                    */
                    return;
                }
                catch (Exception e)
                {
                    string errorMessage = "";

                    // If it's still within retry times limit
                    if (retryTimes < GlobalConstants.retryTimesLimit)
                    {

                        //display this error message to inform the user
                        errorMessage = specificTCPClientName+"にTCP Socketでメッセージを送信できなかった。\n今もう一度送信してみます。\n今回は " + retryTimes + "回目のRetryです。\n\n" + "エラーメッセージ：\n" + e.Message;

                        //output the error message                           
                        //to the App7_エラーメッセージ folder in NAS
                        ReportErrorMsg.outputErrorMsg(specificTCPClientName + "にTCP Socketで送信できなかった", errorMessage);

                        //wait a while before starting next retry
                        Thread.Sleep(GlobalConstants.retryTimeInterval);


                    }

                    //If it has reached the retry limit
                    else
                    {

                        //display this error message to inform the user
                        errorMessage = specificTCPClientName + "にTCP Socketでメッセージを送信できなかった。\n今回は " + retryTimes + "回目のRetryです。\nRetry回数の上限に達しましたので、Retryしません。\n\n" + "エラーメッセージ：\n" + e.Message + "\n\n" + "解決手順：\nStep1　システムの停止ボタンを押してから、もう一度システムの開始ボトルを押して再開してください。";

                        //output the error message                           
                        //to the App7_エラーメッセージ folder in NAS
                        ReportErrorMsg.outputErrorMsg(specificTCPClientName+"にTCP Socketで送信できなかった", errorMessage);

                        //wait a while to let the program output the 3rd retry error txt message
                        //The 3rd retry error message can not be output without this wait 
                        Thread.Sleep(1000);

                        //Stop the アプリ if necessary

                    }
                }
            }
        }


        //Close the TCP socket stream and connection with the client
        public static void cutTCPConnection(NetworkStream networkStream, TcpClient TCPClient)
        {
            if (networkStream != null)
            {
                networkStream.Close();
            }

            if (TCPClient != null)
            {
                TCPClient.Close();
            }
            
        }



        //TCP clientからのメッセージを受け取って該当する処理を実行する。
        //子classによって処理が異なる。
        public abstract void CommunicateWithClient(TcpClient TCPClient);

    }
}

   
