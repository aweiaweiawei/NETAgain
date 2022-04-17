using Net.core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Net.Logic
{   //玩家上线后的临时数据
    public class PlayerTempData
    {
        //玩家状态
            public enum Status
        {
            None,
            Finding,
            Fighting,
        }
        public Status status;
        public Room room;
        public PlayerTempData()
        {
            status = Status.None;
        }

        
    }

}
