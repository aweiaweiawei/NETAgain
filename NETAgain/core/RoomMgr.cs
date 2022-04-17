using Net.Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Net.core
{
    public class RoomMgr
    {
        //单例
        public static RoomMgr instance;
        public RoomMgr()
        {
            instance = this;
        }
        //房间列表
        public List<Room> list = new List<Room>();


        //创建房间
        public void CreateRoom(Player player)
        {
            Room room = new Room();
            lock(list)
            {
                list.Add(room);
                room.AddPlayer(player);
            }
        }
        //玩家离开
        public void LeaveRoom(Player player)
        {
            PlayerTempData tempData = player.tempData;
            if (tempData.status == PlayerTempData.Status.None)
                return;
            Room room =  tempData.room;
            lock(list)
            {
                room.DelPlayer(player.id);
                //房间玩家为空时删除房间
                if(room.list.Count == 0)
                    list.Remove(room);
            }

        }
        //获取房间列表
        public ProtocolBytes GetRoomList()
        {
            ProtocolBytes protocol = new ProtocolBytes();
            protocol.Addstring("GetRoomList");
            int count = list.Count; 
            //房间数量
            protocol.AddInt(count);
            Console.WriteLine("当前房间数"+count);
            //每个房间的信息
            for(int i=0;i<count;i++)
            {
                Room room=list[i];
                protocol.AddInt(room.list.Count);
                protocol.AddInt((int)room.status);
                Console.WriteLine("房间玩家数"+room.list.Count+"房间状态"+ room.status);
            }
            return protocol;

        }
    }
}
