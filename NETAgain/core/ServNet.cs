using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Collections;
using Net.Logic;
using System.Reflection;

namespace Net
{
    public class ServNet
    {
        //监听套接字
        public Socket lisetnfd;
        //客户端连接
        public Conn[] conns;
        //最大连接数
        public int maxConn = 50;

        //协议
        public ProtocolBase proto;

        public static ServNet instance;
        //主定时器
        System.Timers.Timer timer = new System.Timers.Timer(1000);
        //心跳时间
        public long heartBeatTime = 1000;

        //消息分发
        public HandleConnMsg handleConnMsg = new HandleConnMsg();
        public HandlePlayerMsg handlePlayerMsg = new HandlePlayerMsg();
        public HandlePlayerEvent handlePlayerEvent = new HandlePlayerEvent();
        public HandleRoomMsg handleRoomMsg = new HandleRoomMsg();

        public ServNet()
        {
            instance = this;
        }

        //获得最大连接池索引，返回负数表示失败
        public int NewIndex()
        {
            if (conns == null)
                return -1;
            for (int i = 0; i < conns.Length; i++)
            {
                if (conns[i] == null)
                {
                    conns[i] = new Conn();
                    return i;
                }
                else if (conns[i].isUse == false)
                {
                    return i;
                }
            }
            return -1;
        }

        //开启服务器
        public void Start(string host, int port)
        {

            //定时器
            timer.Elapsed += new System.Timers.ElapsedEventHandler(HandleMainTimer);
            timer.AutoReset = false;
            timer.Enabled = true;
            //连接池
            conns = new Conn[maxConn];
            for (int i = 0; i < maxConn; i++)
            {
                conns[i] = new Conn();

            }
            //Socket
            lisetnfd = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            //Bind
            IPAddress ipAdr = IPAddress.Parse(host);
            IPEndPoint ipEp = new IPEndPoint(ipAdr, port);
            lisetnfd.Bind(ipEp);
            //Listen
            lisetnfd.Listen(maxConn);
            //Accept
            lisetnfd.BeginAccept(AcceptCb, null);
            Console.WriteLine("[服务器] 启动成功");
        }

        //Accept回调
        private void AcceptCb(IAsyncResult ar)
        {
            try
            {
                Socket socket = lisetnfd.EndAccept(ar);
                int index = NewIndex();

                if (index < 0)
                {
                    socket.Close();
                    Console.WriteLine("[警告] 连接已满");

                }
                else
                {
                    Conn conn = conns[index];
                    conn.Init(socket);
                    string adr = conn.GetAddress();
                    Console.WriteLine("客户端连接[" + adr + "] conn池ID : " + index);
                    conn.socket.BeginReceive(conn.readBuff, conn.buffCount, conn.BuffRemain(), SocketFlags.None, ReceiveCb, conn);

                }
                lisetnfd.BeginAccept(AcceptCb, null);


            }
            catch (Exception e)
            {
                Console.WriteLine("AcceptCb失败：" + e.Message);
            }
        }
        //Receive回调
        private void ReceiveCb(IAsyncResult ar)
        {
            Conn conn = (Conn)ar.AsyncState;
            try
            {
                int count = conn.socket.EndReceive(ar);
                //关闭信号
                if (count <= 0)
                {
                    Console.WriteLine("收到[" + conn.GetAddress() + "]断开连接");
                    conn.Close();
                    return;
                }
                conn.buffCount += count;
                ProcessData(conn);
                //继续接收，
                conn.socket.BeginReceive(conn.readBuff, conn.buffCount, conn.BuffRemain(), SocketFlags.None, ReceiveCb, conn);


            }
            catch (Exception e)
            {
                Console.WriteLine("收到[" + conn.GetAddress() + "]断开连接");
                conn.Close();
            }
        }
        //实现缓冲区的消息处理(涉及粘包分包处理）
        public void ProcessData(Conn conn)
        {
            if (conn.buffCount < sizeof(Int32))
            {
                return;
            }
            Array.Copy(conn.readBuff, conn.lenBytes, sizeof(Int32));
            conn.msgLength = BitConverter.ToInt32(conn.lenBytes, 0);//?????????conn.lenBytes or conn.readBuffer
            if (conn.buffCount < conn.msgLength + sizeof(Int32))
            {
                return;
            }
            //处理消息
            //协议解码
            ProtocolBase protocol = proto.Decode(conn.readBuff, sizeof(Int32), conn.msgLength);
            //处理解码后的协议
            HandleMsg(conn, protocol);

            int count = conn.buffCount - conn.msgLength - sizeof(Int32);
            Array.Copy(conn.readBuff, sizeof(Int32) + conn.msgLength, conn.readBuff, 0, count);
            conn.buffCount = count;
            if (conn.buffCount > 0)
            {
                ProcessData(conn);

            }

        }

        //实现消息发送
        public void Send(Conn conn, ProtocolBase protocol)
        {
            byte[] bytes = protocol.Encode();
            byte[] length = BitConverter.GetBytes(bytes.Length);
            byte[] sendbuff = length.Concat(bytes).ToArray();
            try
            {
                conn.socket.BeginSend(sendbuff, 0, sendbuff.Length, SocketFlags.None, null, null);


            }
            catch (Exception e)
            {
                Console.WriteLine("[发送消息]" + conn.GetAddress() + ":" + e.Message);
            }
        }
        //广播
        public void Broadcast(ProtocolBase protocol)
        {
            for (int i = 0; i < conns.Length; i++)
            {
                if (!conns[i].isUse)
                    continue;
                //if (conns[i].player == null)
                //   continue;
                Send(conns[i], protocol);
            }
        }

        public void Close()
        {
            for (int i = 0; i < conns.Length; i++)
            {
                Conn conn = conns[i];
                if (conn == null) continue;
                if (!conn.isUse) continue;
                lock (conn)
                {
                    conn.Close();
                }
            }
        }
        //主定时器
        public void HandleMainTimer(object sender, System.Timers.ElapsedEventArgs e)
        {
            //处理心跳
            HeartBeat();
            timer.Start();
        }

        //心跳
        public void HeartBeat()
        {
            // Console.WriteLine("[主定时器执行]");
            long timeNow = Sys.GetTimeStamp();

            for (int i = 0; i < conns.Length; i++)
            {
                Conn conn = conns[i];
                if (conn == null) continue;
                if (!conn.isUse) continue;

                if (conn.lastTickTime < timeNow - heartBeatTime)
                {
                    Console.WriteLine("[心跳引起断开连接]" + conn.GetAddress());
                    lock (conn)
                        conn.Close();
                }
            }
        }
        //不同类型消息的处理
        private void HandleMsg(Conn conn, ProtocolBase protoBase)
        {
            string name = protoBase.GetName();
            string methodName = "Msg" + name;
            //连接协议分发
            //handleConnMsg类
            if (conn.player == null || name == "HeatBeat" || name == "Logout")
            {
                MethodInfo mm = handleConnMsg.GetType().GetMethod(methodName);
                if (mm == null)
                {
                    string str = "[警告] 【登陆前】  HandleMsg没有处理连接方法";
                    Console.WriteLine(str + methodName);
                    return;
                }
                Object[] obj = new Object[] { conn, protoBase };
                Console.WriteLine("[处理连接消息]" + conn.GetAddress() + "；" + name);
                mm.Invoke(handleConnMsg, obj);
            }
            else
            //handleplayerMsg类
            if(name =="GetScore"||name =="AddScore"||name=="AddMsg"||name=="GetMsg"||name=="Problem")
            {
                MethodInfo mm = handlePlayerMsg.GetType().GetMethod(methodName);
                if (mm == null)
                {
                    string str = "[警告]【登陆后】 HandleMsg没有处理玩家方法";
                    Console.WriteLine(str + methodName);
                    return;
                }
                Object[] obj = new Object[] { conn.player, protoBase };
                Console.WriteLine("[处理玩家消息]" + conn.player.id + ":" + name);
                mm.Invoke(handlePlayerMsg, obj);
            }
            else
            //handleRoomMsg类
            {
                MethodInfo mm = handleRoomMsg.GetType().GetMethod(methodName);
                if (mm == null)
                {
                    string str = "[警告]【登陆后】 handleRoomMsg没有处理玩家方法";
                    Console.WriteLine(str + methodName);
                    return;
                }
                Object[] obj = new Object[] { conn.player, protoBase };
                Console.WriteLine("[处理玩家消息]" + conn.player.id + ":" + name);
                mm.Invoke(handleRoomMsg, obj);

            }

        }
    }
}
