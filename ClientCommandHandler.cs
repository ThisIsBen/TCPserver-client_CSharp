using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace DataManagementApp
{
    class ClientCommandHandler
    {

        //Use a lock to prevent the 6 cameras' communicateWithClientThread 
        //trying to adding pictures into their SaveLists at the same time.
        //This situation may happen when 切り替わり occurs at the 6 cameras at the same time .
        private static readonly object AddPicToSaveList_Lock = new object();


        //Use a lock to prevent more than 2 threadpool threads adding pictures to a ValidationList.
        private static readonly object AddPicToValidationList_Lock = new object();





        //Peform the tasks that need to be done before reply to the client.

        //Notice!!!!!!This function must be very short.
        //If it runs more than 5 seconds, the corresponding client(App1-6) 
        //will regard App7 as out of order ,send out pop-up error message, and stop running.
        public void performTaskBeforeReply(string clientCommand)
        {
            if (clientCommand == "終了通知")
            {
                //Indicate that the 締め括り処理 has begun
                //so that App7 will not try to restart the terminated App1～6.
                if (GlobalConstants.DoesUserShutDownTheSystem == false)
                {
                    GlobalConstants.DoesUserShutDownTheSystem = true;
                }


            }
        }


        //画像処理アプリ(App1～6)からのメッセージに従って、該当するタスクを実施する。
        public void handleClientCommand(string clientCameraName, int clientCameraNo, string clientCommand, NetworkStream networkStream, TcpClient TCPClient)
        {
            string logMsg = "";

            //タスク①　繋がっている同じPCの画像処理アプリ(App1～6)を記録し、
            //締め括り処理と同じPCの画像処理アプリ(App1～6)異常停止からの再起動に使う。

            //画像処理アプリ(App1～6)が "システムが起動された時に、画像処理アプリにTCPで繋がった"場合、
            //To know which 画像処理アプリ is connected to the データ管理アプリ
            //and get that 画像処理アプリ's path for restarting it if it crashed.
            if (clientCommand.StartsWith("Connect to " + GlobalConstants.TCPSocketServerName))
            {
                //clientCommandの内容の例として、
                //"Connect to App7|C:\pp\連動ソフト\App1.exe"の場合、
                //本アプリがpp1の異常停止を検知した場合、
                //pp1の保存先C:\pp\連動ソフト\App1.exeを使って再起動させる。

                if (clientCameraName == GlobalConstants.camera1Name)
                {
                    //App1～6を異常停止から自動的に再起動するため、App1～6のパスを記録する
                    GlobalConstants.App1ExePath = clientCommand.Split('|')[1];

                    //App１～６終了ためのアプリの起動停止記号です
                    //TCPで繋がったので、アプリの状態を起動とするので、falseに設定する。
                    //App１～６の終了通知が届いたら、
                    //そのAppの終了記号はtrueになる
                    GlobalConstants.App1End = false;

                }
                else if (clientCameraName == GlobalConstants.camera2Name)
                {
                    //App1～6を異常停止から自動的に再起動するため、App1～6のパスを記録する
                    GlobalConstants.App2ExePath = clientCommand.Split('|')[1];

                    //App１～６終了ためのアプリの起動停止記号です
                    //TCPで繋がったので、アプリの状態を起動とするので、falseに設定する。
                    //App１～６の終了通知が届いたら、
                    //そのAppの終了記号はtrueになる
                    GlobalConstants.App2End = false;

                }
                else if (clientCameraName == GlobalConstants.camera3Name)
                {
                    //App1～6を異常停止から自動的に再起動するため、App1～6のパスを記録する
                    GlobalConstants.App3ExePath = clientCommand.Split('|')[1];

                    //App１～６終了ためのアプリの起動停止記号です
                    //TCPで繋がったので、アプリの状態を起動とするので、falseに設定する。
                    //App１～６の終了通知が届いたら、
                    //そのAppの終了記号はtrueになる
                    GlobalConstants.App3End = false;

                }
                else if (clientCameraName == GlobalConstants.camera4Name)
                {
                    //App1～6を異常停止から自動的に再起動するため、App1～6のパスを記録する
                    GlobalConstants.App4ExePath = clientCommand.Split('|')[1];

                    //App１～６終了ためのアプリの起動停止記号です
                    //TCPで繋がったので、アプリの状態を起動とするので、falseに設定する。
                    //App１～６の終了通知が届いたら、
                    //そのAppの終了記号はtrueになる
                    GlobalConstants.App4End = false;

                }
                else if (clientCameraName == GlobalConstants.camera5Name)
                {
                    //App1～6を異常停止から自動的に再起動するため、App1～6のパスを記録する
                    GlobalConstants.App5ExePath = clientCommand.Split('|')[1];

                    //App１～６終了ためのアプリの起動停止記号です
                    //TCPで繋がったので、アプリの状態を起動とするので、falseに設定する。
                    //App１～６の終了通知が届いたら、
                    //そのAppの終了記号はtrueになる
                    GlobalConstants.App5End = false;

                }
                else if (clientCameraName == GlobalConstants.camera6Name)
                {
                    //App1～6を異常停止から自動的に再起動するため、App1～6のパスを記録する
                    GlobalConstants.App6ExePath = clientCommand.Split('|')[1];

                    //App１～６終了ためのアプリの起動停止記号です
                    //TCPで繋がったので、アプリの状態を起動とするので、falseに設定する。
                    //App１～６の終了通知が届いたら、
                    //そのAppの終了記号はtrueになる
                    GlobalConstants.App6End = false;
                }
            }


            //タスク②　繋がっている他のPCの画像処理アプリ(App1～6)をConsole Windowで表示する。
            //（確認用だけである。）
            else if (clientCommand == "AnotherPC画像処理アプリ接続リクエスト")
            {

                logMsg = clientCameraName + "と繋がりました";
                Console.WriteLine(logMsg);
            }



            //タスク③　オフライン検証用画像保存
            //誤検知への対処用の画像を収集するために、
            //送信してきたカメラのみ初期画像、事後/予兆/ERRへ切り替わったタイミングでの前の画像を保存する。
            else if (clientCommand.StartsWith("送信したカメラのみオフライン検証用画像保存"))
            {
                //clientCommandの内容の例として、
                //"送信したカメラのみオフライン検証用画像保存|01001|10"の場合、
                //基準画像の01001.jpgとその前の10枚の画像00991.jpg～01000.jpgが保存される。

                logMsg = "\n\n\n" + clientCameraName + "から[" + clientCommand + "]を受け取った。\n" +
                clientCameraName + "のみ、オフライン検証用画像を保存する。\n";
                Console.WriteLine(logMsg);


                //オフライン検証用画像保存用の基準画像名と種類が含まれているかどうかを確認
                string[] clientCommand_Detail = clientCommand.Split('|');
                if(clientCommand_Detail.Length < 3)
                {
                    logMsg = clientCameraName + "が送った[" + clientCommand + "]にオフライン検証用画像保存用の\n" +
                    "基準画像名が含まれていないため、保存できない。\n";
                    Console.WriteLine(logMsg);
                    
                    return;
                }
                //オフライン検証用画像保存用の基準画像名と種類を取得
                int basePicName = int.Parse(clientCommand_Detail[1]);
                int numOfPastValidationPicToBeSaved = int.Parse(clientCommand_Detail[2]);

                




                //保存したい画像名を
                //ValidationListに追加する機能を実行する
                lock (AddPicToValidationList_Lock)
                {

                    //予兆と事後が発生した直前、6つのあらかじめ生成されたThreadPool Threadsで
                    //設定枚数分の過去と未来のの画像をValidationListに
                    //移動する                           
                    ManageValidationList manageValidationListObj = new ManageValidationList(clientCameraNo, basePicName, numOfPastValidationPicToBeSaved);

                    //Add 設定枚数分の過去と未来のの画像 to ValidationList 
                    //only if the camera folder is not empty.
                    if (manageValidationListObj.IsCameraFolderEmpty == false)
                    {

                        //警報前後の画像を重複なくValidationListに追加
                        manageValidationListObj.movePicToValidationList();

                    }

                    //To record the time that the latest オフライン検証用画像保存 is handled
                    //We use it to check if it has been more than 12 hours　since the latest オフライン検証用画像保存 is handled.
                    GlobalConstants.latestSaveValidationPicTime = DateTime.Now;

                }

            }



            //タスク④　"全カメラ" 前後画像保存
            //利用場面：全カメラの警報前後の画像保存
            //画像処理アプリ(App1～6)いずれか 警報が"予兆/事後に切り替わった。"場合、
            //各カメラの対して、設定枚数分の予兆/事後の前後の画像のファイル名をSaveListに追加する。
            else if (clientCommand == "全カメラ警報前後画像保存")
            {

                logMsg = "\n\n\n" + clientCameraName + "から[" + clientCommand + "]を受け取った。\n" +
                "全カメラの切り替わり前後画像を保存する。\n";
                Console.WriteLine(logMsg);

                
                //予兆と事後が発生した直前、
                //保存したい画像名を
                //SaveListに追加する機能を実行する
                lock (AddPicToSaveList_Lock)
                {


                    //6つのThreadPool threadsを使って、
                    //各カメラに対して、同時に設定枚数分の警報前後の画像を各カメラのSaveListに追加する。
                    Parallel.For( 1, GlobalConstants.numOfCameraSaveToNAS + 1,
                        cameraNo =>
                        {
                            //If the camera folder path is not set in the 
                            //設定パラメーターTxtファイル,
                            //it means the user doesn't want to save pictures of that camera;
                            //therefore,we do nothing with the pictures taken by that camera.
                            if (GlobalConstants.SaveBeforeAfterAlarmPic[cameraNo] == false)
                            {
                                return;
                            }




                            //設定枚数分の過去と未来のの画像をSaveListに移動する                          
                            ManageSaveList manageSaveListObj = new ManageSaveList(cameraNo);

                            //Add 設定枚数分の過去と未来のの画像 to SaveList 
                            //only if the camera folder is not empty.
                            if (manageSaveListObj.IsCameraFolderEmpty == false)
                            {
                                
                                //警報前後の画像を重複なくSaveListに追加
                                manageSaveListObj.movePastAndFuturePicToSaveList();

                            }

                            //Console.WriteLine("カメラ" + cameraNo + " 正常/予兆/事後切り替わりが発生した直前、NASに保存したい切り替わり前後の画像をSaveListに移動するthread" + " は起動された。");

                        });

                    //To record the time that the latest 切り替わり is handled by adding picture numbers to SaveList
                    //We use it to check if it has been more than 12 hours since the latest 切り替わり is handled.
                    GlobalConstants.latestSaveBeforeAfterAlarmPicTime = DateTime.Now;




                }
                




            }




            //タスク⑤　"切り替わりが発生したカメラのみ" 警報切り替わり前後の画像を保存
            //利用場面：2022/8/26の時点では、利用されていない。
            //切り替わりが発生したカメラのみ、設定枚数分の前後の画像を保存したい時に使える。
            else if (clientCommand == "警報切り替わったカメラのみ警報前後画像保存")
            {


                //If the camera folder path is not set in the 
                //設定パラメーターTxtファイル,
                //it means the user doesn't want to save pictures of that camera;
                //therefore,we do nothing with the pictures taken by that camera.
                if (GlobalConstants.SaveBeforeAfterAlarmPic[clientCameraNo] == false)
                {
                    return;
                }



                logMsg = "\n\n\n" + clientCameraName + "から[" + clientCommand + "]を受け取った。\n" +
                clientCameraName + "のみ、切り替わり前後画像を保存する。\n";
                Console.WriteLine(logMsg);


                //保存したい画像名を
                //SaveListに追加する機能を実行する
                lock (AddPicToSaveList_Lock)
                {

                    //設定枚数分の過去と未来のの画像をSaveListに移動する                      
                    ManageSaveList manageSaveListObj = new ManageSaveList(clientCameraNo);

                    //Add 設定枚数分の過去と未来のの画像 to SaveList 
                    //only if the camera folder is not empty.
                    if (manageSaveListObj.IsCameraFolderEmpty == false)
                    {

                        //警報前後の画像を重複なくSaveListに追加
                        manageSaveListObj.movePastAndFuturePicToSaveList();

                    }

                    //To record the time that the latest 切り替わり is handled by adding picture numbers to SaveList
                    //We use it to check if it has been more than 12 hours since the latest 切り替わり is handled.
                    GlobalConstants.latestSaveBeforeAfterAlarmPicTime = DateTime.Now;

                }

            }



           


            //タスク⑥ 画像処理アプリが送信して来ないエラーを検知する機能（生存確認）
            //利用場面：生存確認機能に設定した画像処理アプリの送信周期の2倍の時間内に、
            //送信して来ない場合、画像処理アプリを異常停止（応答なし）とみなし、再起動させる。
            else if (clientCommand == "生存確認")
            {
                


                //After receiving the first "生存確認" message from TCP Client,
                //we know that 生存確認機能 is activated, so we start checking if 
                //画像処理アプリis alive by checking whether it sends "生存確認" message periodically.
                if (GlobalConstants.DoesPreviousAliveConfirmExist[clientCameraNo])
                {
                   
                    //Reset the timer if データ管理アプリ received the "生存確認" message
                    //from the corresponding 画像処理アプリ within the specified time limit.
                    GlobalConstants.aliveConfirmTimer[clientCameraNo].Change( GlobalConstants.aliveConfirmTimeDiff_Threshold, -1);

                }
                else
                {
                   
                    //To indicate that it is the first time that データ管理アプリ received
                    //"生存確認" message from the corresponding 画像処理アプリ.
                    GlobalConstants.DoesPreviousAliveConfirmExist[clientCameraNo] = true;

                    //Start the timer that will invoke an exception to regard the corresponding 画像処理アプリ
                    //as being crashed if データ管理アプリ didn't receive its
                    //"生存確認" message within the specified time limit.
                    GlobalConstants.aliveConfirmTimer[clientCameraNo]=new Timer(
                    _ => { 
                           GlobalConstants.DoesPreviousAliveConfirmExist[clientCameraNo] = false;
                           GlobalConstants.aliveConfirmTimer[clientCameraNo].Dispose();
                           string errorMessage = clientCameraName + " の画像処理アプリの生存確認メッセージ未送信エラー\n異常停止とみなし、再起動しました。\n"; 
                        　 ReportErrorMsg.showMsgBox_Anyway(errorMessage, "PI製膜システム" + GlobalConstants.TCPSocketServerName + "　応答なし画像処理アプリの再起動");
                           Console.WriteLine(errorMessage);
                           //ネットワーク エラーを起こす。
                           //CommunicateWithTCPClient.cs の TCPCommunicationErrHandlerを実行させ、
                           //異常停止した画像処理アプリを再起動させる。。
                           SocketServer.cutTCPConnection(networkStream, TCPClient); 
                    },
                    null,
                    GlobalConstants.aliveConfirmTimeDiff_Threshold,
                    GlobalConstants.aliveConfirmTimeDiff_Threshold);
                }





            }




            //タスク⑦　締め括り処理の実行
            //画像処理アプリ(App1～6)いずれの "終了通知が届いた"場合、
            //締め括り処理を実行する。
            else if (clientCommand == "終了通知")
            {
                
                //Mark that the App that sent the "終了通知" has stopped.
                //When "終了通知" from all the App1~6 have been received
                //we can run the 締め括り処理.
                if (clientCameraName== GlobalConstants.camera1Name)
                {
                    GlobalConstants.App1End = true;                   
                }
                else if (clientCameraName == GlobalConstants.camera2Name)
                {
                    GlobalConstants.App2End = true;
                }
                else if (clientCameraName == GlobalConstants.camera3Name)
                {
                    GlobalConstants.App3End = true;
                }
                else if (clientCameraName == GlobalConstants.camera4Name)
                {
                    GlobalConstants.App4End = true;
                }
                else if (clientCameraName == GlobalConstants.camera5Name)
                {
                    GlobalConstants.App5End = true;
                }
                else if (clientCameraName == GlobalConstants.camera6Name)
                {
                    GlobalConstants.App6End = true;
                }

                //display which 画像処理アプリ's 終了通知 is received
                logMsg = clientCameraName + "から[" + clientCommand + "]を受け取った。\n";
                Console.WriteLine(logMsg);



                //画像処理アプリ(App1～6)の終了通知全部届いた場合、締め括り処理を実行する
                if (GlobalConstants.App1End && GlobalConstants.App2End && GlobalConstants.App3End && GlobalConstants.App4End && GlobalConstants.App5End && GlobalConstants.App6End)
                {
                    

                    logMsg = "App1～6の終了通知が全部届いた。締め括り処理を実行する。\n保存したい画像を全部NASに保存してから、プログラムを停止する。";
                    Console.WriteLine(logMsg);
                    //Write msg to system log for debug
                    /*
                    EventLogHandler.outputLog(logMsg);
                    */




                    /////締め括り処理を実行する///////


                    //締め括り処理①各カメラの保存したい画像を全部NASにアップロードするまで待つ
                    logMsg = "\n停止する前、保存したい画像を全部NASにアップロードするまで待ちます。";
                    Console.WriteLine(logMsg);
                    waitUntilPicUploadToNAS();


                    //Write msg to system log for debug
                    /*
                    EventLogHandler.outputLog(logMsg);
                    */

                    //締め括り処理②データ管理アプリApp7を停止する
                    Environment.Exit(Environment.ExitCode);
                    
                }


            }


        }





        //締め括り処理①画像処理アプリ(App1～6)の保存したい画像を全部NASにアップロードするまで待つ
        private void waitUntilPicUploadToNAS()
        {
           
            while(true)
            {

                //For debug
                /*
                Console.WriteLine("camera1SaveListNotExistPicCounter" + GlobalConstants.camera1SaveListNotExistPicCounter);
                Console.WriteLine("camera1SaveList.Count - 1" + (GlobalConstants.camera1SaveList.Count - 1).ToString());
                Console.WriteLine("camera2SaveListNotExistPicCounter" + GlobalConstants.camera2SaveListNotExistPicCounter);
                Console.WriteLine("camera2SaveList.Count - 1" + (GlobalConstants.camera2SaveList.Count - 1).ToString());
                Console.WriteLine("camera3SaveListNotExistPicCounter" + GlobalConstants.camera3SaveListNotExistPicCounter);
                Console.WriteLine("camera3SaveList.Count - 1" + (GlobalConstants.camera3SaveList.Count - 1).ToString());
                Console.WriteLine("camera4SaveListNotExistPicCounter" + GlobalConstants.camera4SaveListNotExistPicCounter);
                Console.WriteLine("camera4SaveList.Count - 1" + (GlobalConstants.camera4SaveList.Count - 1).ToString());
                Console.WriteLine("camera5SaveListNotExistPicCounter" + GlobalConstants.camera5SaveListNotExistPicCounter);
                Console.WriteLine("camera5SaveList.Count - 1" + (GlobalConstants.camera5SaveList.Count - 1).ToString());
                Console.WriteLine("camera6SaveListNotExistPicCounter" + GlobalConstants.camera6SaveListNotExistPicCounter);
                Console.WriteLine("camera6SaveList.Count - 1" + (GlobalConstants.camera6SaveList.Count - 1).ToString());
                Console.WriteLine("copyPicToNASNumber" + GlobalConstants.copyPicToNASNumber);
                */



                    //各カメラのSaveListは全部下記の状況のいずれになるまで待つ。
                  　//①SaveListが空になった状態
                    //②SaveList内の画像全部カメラフォルダに存在していない状態
                    //③SaveList内の毎回NASに保存する枚数の画像がカメラフォルダに存在していない状態
                    //例：毎回NASに保存する枚数は5枚で、SaveList内の画像は5枚存在していない場合は該当する。

                    if (   
                           (GlobalConstants.camera1SaveListNotExistPicCounter == GlobalConstants.copyPicToNASNumber || GlobalConstants.camera1SaveListNotExistPicCounter == GlobalConstants.camera1SaveList.Count || GlobalConstants.camera1SaveList.Count == 0)
                        && (GlobalConstants.camera2SaveListNotExistPicCounter == GlobalConstants.copyPicToNASNumber || GlobalConstants.camera2SaveListNotExistPicCounter == GlobalConstants.camera2SaveList.Count || GlobalConstants.camera2SaveList.Count == 0)
                        && (GlobalConstants.camera3SaveListNotExistPicCounter == GlobalConstants.copyPicToNASNumber || GlobalConstants.camera3SaveListNotExistPicCounter == GlobalConstants.camera3SaveList.Count || GlobalConstants.camera3SaveList.Count == 0)
                        && (GlobalConstants.camera4SaveListNotExistPicCounter == GlobalConstants.copyPicToNASNumber || GlobalConstants.camera4SaveListNotExistPicCounter == GlobalConstants.camera4SaveList.Count || GlobalConstants.camera4SaveList.Count == 0)
                        && (GlobalConstants.camera5SaveListNotExistPicCounter == GlobalConstants.copyPicToNASNumber || GlobalConstants.camera5SaveListNotExistPicCounter == GlobalConstants.camera5SaveList.Count || GlobalConstants.camera5SaveList.Count == 0)
                        && (GlobalConstants.camera6SaveListNotExistPicCounter == GlobalConstants.copyPicToNASNumber || GlobalConstants.camera6SaveListNotExistPicCounter == GlobalConstants.camera6SaveList.Count || GlobalConstants.camera6SaveList.Count == 0)
                       )
                    {
                        //proceed to the 締め括り処理 to finish this program
                        return;
                    }
   
                
                //wait for 1 second and check again to see if all the 
                //unsaved pictures in  SaveLsit are moved to NAS
                Thread.Sleep(1000);
            }
            


        }
        
    }
}
