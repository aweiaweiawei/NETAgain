using Net.core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Net.Logic
{
    public class HandleRoomMsg
    {
		//获取玩家成就（当前即胜率）
		public void MsgGetAchieve(Player player, ProtocolBase protoBase)
		{
			ProtocolBytes protocolRet = new ProtocolBytes();
			protocolRet.Addstring("GetAchieve");
			protocolRet.AddInt(player.data.win_rate);
			player.Send(protocolRet);
			Console.WriteLine("MsgGetAchieve " + player.id + player.data.score);

		}
		//获取房间列表
		public void MsgGetRoomList(Player player, ProtocolBase protocol)
        {
			player.Send(RoomMgr.instance.GetRoomList());
        }

		//创建房间
		public void MsgCreateRoom(Player player,ProtocolBase protocolBase)
        {
			ProtocolBytes protocol = new ProtocolBytes();
			protocol.Addstring("EnterRoom");
			//条件检测
			if(player.tempData.status!=PlayerTempData.Status.None)
            {
				Console.WriteLine("[MsgCreateRoom]创建房间失败" + player.id);
				protocol.AddInt(-2);
				player.Send(protocol);
				return; 
            }
			RoomMgr.instance.CreateRoom(player);
			//匹配中
			protocol.AddInt(0);
			player.Send(protocol);
			Console.WriteLine("[MsgCreateRoom]创建房间成功 匹配中 等待玩家其他加入" + player.id);
        }
		//获取房间信息
		public void MsgGetRoomInfo(Player player,ProtocolBase protocolBase)
        {
			if (player.tempData.status != PlayerTempData.Status.Finding) ;
            {
				Console.WriteLine("MsgGetRoomINfo statua err"+player.id);
				return;
            }
			Room room = player.tempData.room;
			player.Send(room.GetRoomInfo());
        }
		//加入房间
		public void MsgEnterRoom(Player player,ProtocolBase protocolBase)
        {
			//获取数值
			int start = 0;
			ProtocolBytes protocol =(ProtocolBytes)protocolBase;
			string protoName = protocol.GetString(start,ref start);
			//初始化使用一个无效的索引号
			int index=99999;
			bool flag = false;
			for(int i=0;i<RoomMgr.instance.list.Count;i++)
            {
				if(RoomMgr.instance.list[i].status==Room.Status.Prepare)
                {
					index = i;
					flag = true;
					

                }
            }
			//如果当前不存在状态为prepare的房间
			//则当前玩家自己创建一个房间
			if(flag==false)
            {   
				MsgCreateRoom(player,protocolBase);
				return;

            }
			//int index = protocol.GetInt(start,ref start);
			Console.WriteLine("收到MsgEnterRoom" + player.id );
			//
			protocol = new ProtocolBytes();
			protocol.Addstring("EnterRoom");
			//判断房间是否存在
			if(index <0||index>=RoomMgr.instance.list.Count)
            {
				Console.WriteLine("MsgEnterRoom index err" + player.id);
				protocol.AddInt(-1);
				player.Send(protocol);
				return;

            }
			Room room =RoomMgr.instance.list[index];
			//判断房间的状态
			if(room.status != Room.Status.Prepare)
            {
				Console.WriteLine("[MsgEnterRoom] status err 进入房间失败"+ player.id);
				protocol.AddInt(-1);
				player.Send(protocol) ;
				return;
            }
			//添加玩家
			if(room.AddPlayer(player))
            {
				room.Broadcast(room.GetRoomInfo());
				room.status = Room.Status.Fight;
				
				protocol.AddInt(1);
				player.Send(protocol);
				Console.WriteLine("[MsgEnterRoom]匹配成功,对弈即将开始");
			}
            else
            {  

				Console.WriteLine("MsgEnterRoom maxPlayer err"+player.id);
				protocol.AddInt(-1);
				player.Send(protocol);

            }
			
        }
		//离开房间
		public void MsgLeaveRoom(Player player,ProtocolBase protoBase)
        {
			ProtocolBytes protocol = new ProtocolBytes();
			protocol.Addstring("LeaveRoom");
			//条件检测
			if(player.tempData.status!=PlayerTempData.Status.Finding)
            {
				Console.WriteLine("MsgLeaveRoom Status err"+player.id);
				protocol.AddInt(-1);
				player.Send(protocol);
				return ;

            }
			//处理
			protocol.AddInt(0);
			player.Send(protocol);
			Room room = player.tempData.room;
			RoomMgr.instance.LeaveRoom(player);
			//广播
			//if(room != null)
            //{
				//room.Broadcast(room.GetRoomInfo());
           //  }
        }

	}
}
