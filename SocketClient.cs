using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace アプリ
{
    class SocketClient
    {
            //Store the connected TCP server and its network stream
            private TcpClient TCPServer;
            private NetworkStream networkStream;


            //For displaying the connected TCP server name in error messages 
            private string TCPServerName;
            //To enable the derived class to access server IP and port,
            //we set them public
            public string TCPServerIP;
            public int TCPServerPort;



            //TCP Serverに送信する時に、送信内容を保存するためのbyte array。
            //byte arrayのサイズは、一回の送信内容のbyte数より大きいことが必要である。
            private Byte[] sendMsgBuffer = new Byte[256];



            //TCP Serverから受信する時に、一回の受信内容を保存するためのbyte array。
            //byte arrayのサイズは、一回の受信内容のbyte数より大きいことが必要である。
            private Byte[] receiveMsgBuffer = new Byte[256];
            //TCP Serverから受信する時に、一回の受信内容に対して、正常に読み込んだ受信内容のbyte数を保存するための変数
            private int receivedMsgLength;
            //TCP Serverから受信した内容を保存するための変数
            private string messageFromServer = String.Empty;
        






            //TCP Server(App7)に繋げる。
            //連結が成功した場合、返り値をtrueにする。
            //Retryしても連結が失敗した場合、エラー処理を実施して、返り値をfalseにする。
            public bool Connect(string serverIP, int port, string cameraName,string serverName)
            {
                //initialize the displayed TCP server name in error messages
                TCPServerName = serverName;
                TCPServerIP = serverIP;
                TCPServerPort = port;
                string logMsg;

                //Retry  when error occurs 
                for (int retryTimes = 1; retryTimes <= GlobalConstants.retryTimesLimit; retryTimes++)
                {
                
                    try
                    {

                        //TcpClient constructor will connect to the specified port on the specified host(App7.exe).
                        //It will stop here until it either connects or fails. 
                        TCPServer = new TcpClient(serverIP, port);
                        //When it successfully connected to App7.exe,
                        //we get the NetworkStream for sending and receiving with App7.exe. 
                        networkStream = TCPServer.GetStream();
                        logMsg = "IP=" + serverIP + " Port=" + port + "の" + TCPServerName + " にTCP socketで繋がりました。";
                        Console.WriteLine(logMsg);

                        // Write msg to system log for outputErrorMsg
                        /*
                            EventLogHandler.outputLog(logMsg);
                        */

                        //連結が成功したため、返り値をtrueにする。
                        return true;

                    }
                    //TCP socketで連結できない場合
                    catch (Exception e)
                    {

                            //Close the TCP socket stream and connection with the server.
                            cutTCPConnection();
               

                            // If it's still within retry times limit,retry again.
                            if (retryTimes < GlobalConstants.retryTimesLimit)
                            {
                                //メッセージがNASの容量対策のため、Retryのメッセージを出さないようにする。
                                /*
                                logMsg = "TCP socket connect to server exception:" + e.Message+ "\n今もう一度接続してみます。\n今回は " + retryTimes + "回目のRetryです。\n\n" ; 

                                ReportErrorMsg.outputErrorMsg("アプリ１～6と"+ TCPServerName + "との連結", logMsg);
                                */

                                //wait for a while before next retry
                                Thread.Sleep(GlobalConstants.retryConnectToTCPSocketServerTimeInterval);
                            }

                            // If it has exceeded the retry times limit, stop retry and display pop-up error message
                            // If the TCP server that crashed is App7.exe, terminate this program.
                            else
                            { 
                                



                            
                                //Give up receiving TCP message to TCP server
                                //end the program here if the TCP server that crashed is App7.exe
                                //アプリ1～6はここで終了
                                if(TCPServerName==GlobalConstants.App7TCPSocketServerName)
                                {

                                        //output the error message                            
                                        //to the AppN(N=1~6)_エラーメッセージ folder on NAS
                                        logMsg = "IP = " + serverIP + " Port = " + port + "の" + TCPServerName + "　に連結できなかった。\n\n" + "エラーメッセージ：\nTCP socket connect to server exception:" + e.Message + "\n今回は " + retryTimes + "回目のRetryです。\nRetry回数の上限に達しましたので、Retryしません。\n\n対象は連結しないといけない" + GlobalConstants.App7TCPSocketServerName + "のため、\n" + GlobalConstants.cameraName + "のソフトウェアは自動的に停止する。 \n\n" + "解決手順：\nStep1 連結対象アプリがちゃんと実行されてから、このアプリを実行してください。\n\n";
                                        ReportErrorMsg.outputErrorMsg("アプリ１～6と" + TCPServerName + "との連結", logMsg);

                                        //wait a while to let the program output the 3rd retry error txt message
                                        //The 3rd retry error message can not be output without this wait 
                                        Thread.Sleep(1000);



                                        //show a pop-up message to inform the operator to reboot the system
                                        //because DataManagementApp or App8 is not activated.
                                        ReportErrorMsg.showMsgBox_Anyway("IP = " + serverIP + " Port = " + port + "の" + TCPServerName + "　に連結できなかった。\n\n"+GlobalConstants.cameraName + "のソフトウェアは自動的に停止した。\n\n停止ボタンを押して、\nを停止してください。\n\n再起動してもこのエラーが続けている場合、\nを停止して管理者に連絡してください。", " " + "App" + GlobalConstants.cameraNo + "_" + TCPServerName + "と連結できないエラー");

                            　　　　    //このアプリを停止させる
                                        Environment.Exit(Environment.ExitCode);
                                }
                                else
                                {


                                        //output the error message                            
                                        //to the AppN(N=1~6)_エラーメッセージ folder on NAS
                                        logMsg = "IP = " + serverIP + " Port = " + port + "の" + TCPServerName + "　に連結できなかった。\n\n連結せず、画像処理を継続していく。" + "エラーメッセージ：\nTCP socket connect to server exception:" + e.Message + "\n今回は " + retryTimes + "回目のRetryです。\nRetry回数の上限に達しましたので、Retryしません。\n\n" + "解決手順：\nStep1 連結対象アプリがちゃんと実行されてから、このアプリを実行してください。\n\n";
                                        ReportErrorMsg.outputErrorMsg("アプリ１～6と" + TCPServerName + "との連結", logMsg);

                                        //wait a while to let the program output the 3rd retry error txt message
                                        //The 3rd retry error message can not be output without this wait 
                                        Thread.Sleep(1000);



                                        //show a pop-up message to inform the operator to reboot the system
                                        //because DataManagementApp or App8 is not activated.
                                        ReportErrorMsg.showMsgBox_Anyway("IP = " + serverIP + " Port = " + port + "の" + TCPServerName + "　に連結できなかった。\n\n" +
                                        "停止ボタンを押して、\nを停止してください。\n\n"+
                                        "\n\n\n\n解決手順：\n\n" +
                                        "もう一台のPCと連結する設定になっているため、\n" +
                                        "もう一台のPCを起動し、\nデスクトップにあるのアイコンをクリックして、\nの画面が表示されてから、\n"+
                                        "このPCのを再起動すれば、解決できる。" +
                                        "\n\nもう一台のPCと連結したくない場合、\nこのPCのを停止して管理者に連絡し、\nこのPCの連動ソフトApp1～6の設定ファイルを編集してもらってください。", " " + "App" + GlobalConstants.cameraNo + "_" + TCPServerName + "と連結できないエラー");

                                }

                                
                            }


                    }
             
                }

                //Retryしても連結が失敗した場合、返り値をfalseにする。
                return false;
            }




        　　//TCP Serverに送信する。
            public void sendMessageToServer(string senderName, string messageToServer)
            {
                //Retry  when error occurs
                for (int retryTimes = 1; retryTimes <= GlobalConstants.retryTimesLimit; retryTimes++)
                {
                    try
                    {
                        // Translate the Message into UTF8 Byte and save it in a byte array buffer.
                        sendMsgBuffer = Encoding.UTF8.GetBytes(senderName + ">" + GlobalConstants.cameraNo + ">" + messageToServer);
                        
                        // Send the message in the byte array buffer to the connected TCP Server. 
                        networkStream.Write(sendMsgBuffer, 0, sendMsgBuffer.Length);

                        break;
                    }
                    //TCP Socketでメッセージを送信できなかった場合
                    catch (Exception e)
                    {
                       


                        string errorMessage = "";
                        // If it's still within retry times limit,retry again.
                        if (retryTimes < GlobalConstants.retryTimesLimit)
                        {
                            //メッセージがNASの容量対策のため、Retryのメッセージを出さないようにする。
                            /*
                    　　    //display this error message to inform the user
                            errorMessage = TCPServerName+" にTCP Socketでメッセージを送信できなかった。\n今もう一度送信してみます。\n今回は " + retryTimes + "回目のRetryです。\n\n" + "エラーメッセージ：\n" + e.Message;

                            //output the error message                            
                            //to the AppN(N=1~6)_エラーメッセージ folder on NAS
                            ReportErrorMsg.outputErrorMsg(TCPServerName+" にTCP Socketで送信できなかった", errorMessage);
                            */
                            //wait a while before starting next retry
                            Thread.Sleep(GlobalConstants.retryTimeInterval);
                        　　

                        }

                        //If it has reached the retry limit
                        else
                        {

                            //display this error message to inform the user
                            errorMessage = TCPServerName+" にTCP Socketでメッセージを送信できなかった。\n今回は " + retryTimes + "回目のRetryです。\nRetry回数の上限に達しましたので、Retryしません。\n\n対象は"+ GlobalConstants.App7TCPSocketServerName + "の場合、\n" + GlobalConstants.cameraName + "のソフトウェアは自動的に停止する。\n\n" + "エラーメッセージ：\n" + e.Message + "\n\n" + "解決手順：\nStep1　システムの停止ボタンを押してから、もう一度システムの開始ボトルを押して再開してください。";

                            //output the error message                            
                            //to the AppN(N=1~6)_エラーメッセージ folder on NAS
                            ReportErrorMsg.outputErrorMsg(TCPServerName + " にTCP Socketで送信できなかった", errorMessage);

                            //wait a while to let the program output the 3rd retry error txt message
                            //The 3rd retry error message can not be output without this wait 
                            Thread.Sleep(1000);



                            
                            //Give up receiving TCP message to TCP server
                            //end the program here if the TCP server that crashed is App7.exe
                            //アプリ1～6はここで終了
                        　　if (TCPServerName==GlobalConstants.App7TCPSocketServerName)
                            {
                                //show a pop-up message to inform the operator to reboot the system
                                //because this App can not send message to DataManagementApp or App8.
                                ReportErrorMsg.showMsgBox_Anyway(GlobalConstants.cameraName + "のソフトウェアはIP = " + TCPServerIP + " Port = " + TCPServerPort + "の　" + TCPServerName + "　に"+ GlobalConstants.retryTimesLimit + "回Retryしても送信できません。\n\n" + TCPServerName + "が異常で停止したため、"+ GlobalConstants.cameraName + "のソフトウェアも自動的に停止した。\n\n停止ボタンを押して、\nを再開してください。\n\n再起動してもこのエラーが続いている場合、\nを停止して管理者に連絡してください。", " " + "App" + GlobalConstants.cameraNo + "_" + TCPServerName + "に送信できないエラー");

                                Environment.Exit(Environment.ExitCode);
                            }
                            else
                            {
                                //show a pop-up message to inform the operator to reboot the system
                                //because this App can not send message to DataManagementApp or App8.
                                ReportErrorMsg.showMsgBox_Anyway(GlobalConstants.cameraName + "のソフトウェアはIP = " + TCPServerIP + " Port = " + TCPServerPort + "の　" + TCPServerName + "　に"+ GlobalConstants.retryTimesLimit + "回Retryしても送信できません。\n" + TCPServerName + "が異常で停止しました。" + "\n\n停止ボタンを押して、\nを再開してください。\n\n再起動してもこのエラーが続いている場合、\nを停止して管理者に連絡してください。", " " + "App" + GlobalConstants.cameraNo + "_" + TCPServerName + "に送信できないエラー");

                            }

                            return;

                        }
                    }
                }

            //only print 全カメラ警報前後画像保存,警報切り替わったカメラのみ警報前後画像保存,and 終了通知 message to console window
            if (messageToServer == "全カメラ警報前後画像保存" || messageToServer == "警報切り替わったカメラのみ警報前後画像保存" || messageToServer == "終了通知")
                {
                    string logMsg = GlobalConstants.cameraName + " sends message to TCPServerIP=" + TCPServerIP + " Port=" + TCPServerPort + "の" + TCPServerName + " : " +"["+ messageToServer+"]";
                    Console.WriteLine(logMsg);
                    
                    // Write msg to system log for outputErrorMsg
                    /*
                      EventLogHandler.outputLog(logMsg);
                    */
                }


            }





            //TCP Serverから受信する。(自身が送信した返事を受信する)
            public void receiveMessageFromServer(string TCPServerIP, int TCPServerPort,string messageToServer)
            {
                //Retry  when error occurs
                for (int retryTimes = 1; retryTimes <= GlobalConstants.retryTimesLimit; retryTimes++)
                {
                    try
                    {

                    

                       
                        //Set a timeout for reading.
                        //If the read operation does not complete within the 
                        //time specified by this property, 
                        //the read operation throws an IOException.
                        networkStream.ReadTimeout = GlobalConstants.TCPSocketServerResponseTimeout;
                        
                        //Read the TCP server response message.
                        receivedMsgLength = networkStream.Read(receiveMsgBuffer, 0, receiveMsgBuffer.Length);
                        messageFromServer = Encoding.UTF8.GetString(receiveMsgBuffer, 0, receivedMsgLength);

                    

                        //If we can reach here,it means we have received the message from the server
                        return;

                    }
                　  //TCP serverからの返事が来なかった場合
                    //If the 受け取り確認 response from the TCP server
                    //does not come within 3 seconds,we regard it as 返事が来ないエラー.
                    catch (Exception e)
                    {
                        //If the message sent to the TCP server is "終了通知",
                        //we do not need to retry and output error message.
                        //Because the TCP server might be shut down 
                        //before we read its TCP socket response message of the "終了通知".
                        if (messageToServer== "終了通知" && (e.Message == "転送接続からデータを読み取れません: 既存の接続はリモート ホストに強制的に切断されました。。" || e.Message == "転送接続からデータを読み取れません: 接続済みの呼び出し先が一定の時間を過ぎても正しく応答しなかったため、接続できませんでした。または接続済みのホストが応答しなかったため、確立された接続は失敗しました。。"))
                        {
                            return;
                        }
                        


                        string errorMessage = "";

                        // If it's still within retry times limit, retry again.
                        if (retryTimes < GlobalConstants.retryTimesLimit)
                        {
                            
                            //wait a while before starting next retry
                            Thread.Sleep(GlobalConstants.retryTimeInterval);
                            

                        }

                        //If it has reached the retry limit,
                        //we stop this program.
                        else
                        {
                            //Display DataManagementApp crash pop-up error message if necessary
                            //show a pop-up message to inform the operator to reboot the system
                            //because this App can not receive response message from DataManagementApp or App8.
                            ReportErrorMsg.showMsgBox_Anyway("IP = " + TCPServerIP + " Port = " + TCPServerPort + "の" + TCPServerName + "　からの返信が" + GlobalConstants.cameraName + "のソフトウェアに"+ GlobalConstants.retryTimesLimit + "回Retryしても、\n返信待ちTimeout:" + GlobalConstants.TCPSocketServerResponseTimeout + "ミニ秒以内に来なかった。\n\n停止ボタンを押して、\nを再開してください。\n\n再起動してもこのエラーが続いている場合、\nを停止して管理者に連絡してください。", " " + "App" + GlobalConstants.cameraNo + "_" + TCPServerName + "からの返信が返信待ちTimeout以内に来なかったエラー");

                            


                        　　//display this error message to inform the user
                            errorMessage = TCPServerName+" の返信がこのアプリに設定された返信待ちTimeout:"+ GlobalConstants.TCPSocketServerResponseTimeout+"ミニ秒以内に来なかった。\n\n今回は " + retryTimes + "回目のRetryです。\nRetry回数の上限に達しましたので、Retryしません。\n\nエラーメッセージ：\n" + e.Message + "\n\n" + "解決手順：\nStep1　システムの停止ボタンを押してから、もう一度システムの開始ボトルを押して再開してください。";

                            //output the error message                        
                            //to the AppN(N=1~6)アプリ_エラーメッセージ folder on NAS
                            ReportErrorMsg.outputErrorMsg(TCPServerName+ " の返信が返信待ちTimeout以内に来なかった", errorMessage);

                            //wait a while to let the program output the 3rd retry error txt message
                            //The 3rd retry error message can not be output without this wait 
                            Thread.Sleep(1000);

                        }

                    }
                }

            
            }





            //Close the TCP socket stream and connection with the server
            public void cutTCPConnection()
            {       
                if(networkStream!=null)
                {
                    networkStream.Close();
                }

                if (TCPServer != null)
                {
                    TCPServer.Close();
                }
                
            }
        



    }
}
