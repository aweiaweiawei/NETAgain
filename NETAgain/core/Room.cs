using Net.Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Net.core
{  //房间
    public class Room
    {    //房间状态
        public enum Status
        {   //准备状态
            Prepare = 1,
            Fight =2,
        }
        public Status status = Status.Prepare;
        public int maxPlayers = 2;
        //字典存放房间玩家信息
        public Dictionary<string , Player> list = new Dictionary<string , Player>();
        //房间添加玩家
        public bool AddPlayer(Player player)
        {
            lock(list)
            {
                if (list.Count >= maxPlayers)
                {    status = Status.Fight;
                     return false;
                }
                PlayerTempData tempdata = player.tempData;
                //tempdata.room = this;
                tempdata.status = PlayerTempData.Status.Fighting;


                string id = player.id;
                list.Add(id, player);
                   


            }
            return true;
        }
        //房间删除玩家
        public void DelPlayer(string id)
        {
            lock(list)
            {
                if (!list.ContainsKey(id))
                    return;
                list[id].tempData.status = PlayerTempData.Status.None;
                list.Remove(id);

            }
        }

        //房间信息
        public ProtocolBytes GetRoomInfo()
        {
            ProtocolBytes protocol = new ProtocolBytes();
            protocol.Addstring("GetRoomInfo");
            //房间信息
            protocol.AddInt(list.Count);

            //每个玩家的信息
            foreach(Player p in list.Values)
            {  //暂定id,胜率
                protocol.Addstring(p.id);
                protocol.AddInt(p.data.win_rate);
                
            }
            return protocol;    
        }




        //广播
        public void Broadcast(ProtocolBase protocol)

        {
            foreach (Player player in list.Values)
            {
                player.Send(protocol);
            }
        }
            




    }
}
