using System;
using System.Threading;

namespace アプリ
{
    //Inherit the properties and methods from the SocketClient class
    class CommunicateWithTCPServer :SocketClient
    {


        //TCP Server()に繋げる。
        public void ConnectToTCPServer(String TCPSocketServerIP,Int32 TCPSocketServerPort,string sendToServerMsg,string TCPServerName,string cameraName)
        {


            //Connect to TCP Server(App7)
            //連結が成功した場合、返り値をtrueにする。
            //Retryしても連結が失敗した場合、エラー処理を実施して、返り値をfalseにする。
            bool IsConnected = Connect(TCPSocketServerIP, TCPSocketServerPort, cameraName, TCPServerName);

            //If we successfully connected to the TCP Server(App7),
            if (IsConnected == true)
            {
                //Send the initial message to the App7
                //so that the App7 can set the 起動停止記号 of this 画像処理アプリ(Any of the App1～6) to "起動”
                //By doing so, the 生存確認機能 of the App7 can know the 起動停止status of each 画像処理アプリ(Any of the App1～6).
                SendMsg_AndGetReply(cameraName, sendToServerMsg);
            }
            



        }


        //メッセージをTCP Serverに送って、そしてTCP Serverからの確かに受け取った返信を受け取る。
        public void SendMsg_AndGetReply(string cameraName, string messageToServer)
        {
            //メッセージをTCP Serverに送る
            sendMessageToServer(cameraName,messageToServer);


            //Proceed to the next process after receiving a response from TCP server. 

            //Receive a response from TCP server to make sure TCP server indeed got the message.
            receiveMessageFromServer(TCPServerIP, TCPServerPort, messageToServer);

            
        }



        //定期的にApp7に生存確認メッセージを送信する。
        //Keep sending TCP message to TCP servers periodically 
        //In this way, we can display pop-up error message as soon as they crash.
        public  void keepConfrimTCPServerIsAlive()
        {
            
            
            while (true)
            {
                //Send whatever you like to the TCP server
                //except for ”切り替わり”、”終了通知”
                SendMsg_AndGetReply(GlobalConstants.cameraName, "生存確認");
                
                

                //Wait for 生存確認メッセージを送信する周期
                Thread.Sleep(GlobalConstants.checkIsDataManagementAppAliveTimeInterval);
            }
            

        }




    }
}
