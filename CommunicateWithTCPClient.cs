using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace App
{
    class CommunicateWithTCPClient :SocketServer
    {

       

        //親classのconstructorを呼び出して、TCP Serverの初期化を実行する。
        public CommunicateWithTCPClient(string ip, int port, string serverName):base(ip,  port,  serverName)
        {
            //子class専用の処理はない。
        }


        //画像処理アプリ(App1～6)からのメッセージを受け取って、受け取り確認メッセージを返信する。
        //(プログラムが停止するまでずっと画像処理アプリ(App1～6)からのメッセージを待っている。)
        public override void CommunicateWithClient(TcpClient TCPClient)
        {


            //Get the network stream used to send and receive data with the TCP client.
            NetworkStream networkStream = TCPClient.GetStream();

            


            //Store the message received from a client:
            string messageFromClient = null;


            //TCP Serverから受信する時に、一回の受信内容を保存するためのbyte array。
            //サイズは、一回の受信内容のbyte数より小さい場合、
            //相手のTCPは自動的に、残りの受信内容をN回分けて送ってくれる。
            //N=Ceiling(受信内容Byte数/receiveMsgBufferのサイズ)

            //しかし、本システムのbyte arrayのサイズは、
            //一回の受信内容のbyte数より大きいことを前提とする。          
            Byte[] receiveMsgBuffer = new Byte[256];
            //TCP Serverから受信する時に、一回の受信内容に対して、正常に読み込んだ受信内容のbyte数を保存するための変数
            int receivedMsgLength;

            //----------------------------------------


            //Store the message to be sent to a client:
            //Store log message
            string logMsg = "";
            string messageToClient;
            //TCP Serverに送信する時に、送信内容を保存するためのbyte array。
            //byte arrayのサイズは、一回の送信内容のbyte数より大きいことが必要である。
            Byte[] sendMsgBuffer = new Byte[256];
            //Store the camera number,
            //and the command sent from the client
            string clientCameraName = "";
            int clientCameraNo = 0;
            string clientCommand = "";



            //Hanlde the task according to the client message   
            ClientCommandHandler clientCommandHandlerObj = new ClientCommandHandler();

            try
            {
                //Keep waiting for messages from a TCP client(画像処理アプリ)

                //NetworkStream.Read will return 0 if and only if the connection has been terminated,
                //otherwise it will stop here until there is at least one byte to be read.

                //This method reads data into receiveMsgBuffer and returns the number of bytes successfully read.
                //The Read operation reads as much data as is available, up to the number of bytes specified.
                while ((receivedMsgLength = networkStream.Read(receiveMsgBuffer, 0, receiveMsgBuffer.Length)) > 0)
                {
                    //receive message from client(App1-6)
                    messageFromClient = Encoding.UTF8.GetString(receiveMsgBuffer, 0, receivedMsgLength);
                    //extract the message content to get client camera name and the command
                    string[] clientMessage = messageFromClient.Split('>');
                    clientCameraName = clientMessage[0];
                    clientCameraNo = int.Parse(clientMessage[1]);
                    clientCommand = clientMessage[2];



                    //Notice!!!!!!This function must be very short.
                    //If it runs more than 5 seconds, the corresponding client(App1-6) 
                    //will regard App7 as out of order ,send out pop-up error message, and stop running.
                    #region Peform the tasks that need to be done before reply to the client

                    clientCommandHandlerObj.performTaskBeforeReply(clientCommand);

                    #endregion


                    //Send the response message back to the App whose message has been received.
                    messageToClient = clientCameraName + "が送ったメッセージ" + clientCommand + " は" + TCPServerName + " に届きました。";
                    sendMsgBuffer = Encoding.UTF8.GetBytes(messageToClient);
                    //Send the response message back to the App whose message has been received.
                    networkStream.Write(sendMsgBuffer, 0, sendMsgBuffer.Length);



                    //Print out For Debug ********************
                    /*
                     //only record the reception of 切り替わり　and 終了通知 message to system log
                    if (clientCommand == "全カメラ前後画像保存" || clientCommand == "切り替わりが発生したカメラのみ前後画像保存" || clientCommand == "終了通知")
                    {

                        //display the current socket server thread that handles this connection.
                        //Console.WriteLine("{1}: Received: {0}", clientMessage, Thread.CurrentThread.ManagedThreadId);
                        logMsg = "\n"+TCPServerName + " received:" +"["+ clientCommand+"]" + " from " + clientCameraName;
                        Console.WriteLine(logMsg);

                        //Write msg to system log for debugging
                        //EventLogHandler.outputLog(logMsg);



                        //display the current socket server thread that handles this connection.        
                        logMsg = TCPServerName + " sends:" + "["+messageToClient +"]"+ " to " + clientCameraName;
                        Console.WriteLine(logMsg);

                        //Write msg to system log for debugging
                        //EventLogHandler.outputLog(logMsg);


                    }
                    */
                    //Print out For Debug ********************




                    #region Handle the task according to the client message 
                    //画像処理アプリ(App1～6)からのメッセージに従って、該当するタスク（警報前後の画像をNASに保存、締め括り処理の実施など）を実施する。





                    //Try dispatching the handling of the task to a ThreadPool thread
                    //to make App7 finish client command faster to prevent the buffer of 
                    //the TcpClient(default size: 8192 bytes) of App1～6 from overflow.
                    //If the buffer of the TcpClient of App1～6  overflows, 
                    //送信できないエラー　will occur on　App1～6.

                    //Activate and invoke a ThreadPool thread to run the client command.
                    Task.Run(() =>clientCommandHandlerObj.handleClientCommand(clientCameraName, clientCameraNo, clientCommand, networkStream, TCPClient));


                    #endregion




                }

                //show a pop-up message to inform the operator to reboot the system
                ReportErrorMsg.showMsgBox_Anyway(GlobalConstants.TCPSocketServerName + "はApp1-6とのTCP通信機能が異常で停止しまった。" + "\n\n停止ボタンを押して、\nを再開してください。\n\n再起動してもこのエラーが続ける場合、\n停止ボタンを押してを停止して、\n管理者に連絡してください。", " " + GlobalConstants.TCPSocketServerName + "_App1 - 6とのTCP通信機能異常停止エラー");

                //output the error message 
                //to the DataManagementApp_エラーメッセージ folder in NAS
                ReportErrorMsg.outputErrorMsg("App1 - 6とのTCP通信機能異常", "App1-6とのTCP通信機能が異常で停止しまった。再起動が必要です。");
            }

            //Handle a excption that happens during the communication with a TCP client
            catch (Exception e)
            {

                TCPCommunicationErrHandler(TCPClient, networkStream, clientCameraName, e.Message);

            }
        }


        //If the connection with one of the clients(App1-6) is crashed,
        //we close the TCP socket connection with that crashed App
        //and try to restart that crashed App.
        public void TCPCommunicationErrHandler(TcpClient TCPClient, NetworkStream networkStream, string clientCameraName,string errorMessage)
        {
            //Close the TCP socket stream and connection with the client.
            cutTCPConnection(networkStream, TCPClient);


            //We have applied some mechanisms to prevent restarting App when the system is stopped by the user,
            //but sometimes it doesn't work.
            //As a result,we make the thread that is used to communicate with its App client to wait for a while before restart the crashed App
            //to prevent restarting App when the system is stopped by the user.
            Thread.Sleep(3000);


            //App1～6を異常停止から自動的に再起動する
            //(Retry上限に達しても、まだ異常で停止した場合、再起動するのを諦める。)
            RestartAppFromCrash.restartCrashedImgInspectionApp(clientCameraName, errorMessage);

        }


    }
}
