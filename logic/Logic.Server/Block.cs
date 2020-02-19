﻿using System;
using System.Collections.Generic;
using System.Text;
using static Logic.Constant.MapInfo;
using System.Configuration;
using Logic.Constant;
using Communication.Proto;

namespace Logic.Server
{
    public class Block : Obj
    {
        public int RefreshTime;//食物刷新点的食物刷新速率，毫秒


        public List<DishType> Task = null;//任务点的任务列表
        public Block(double x_t, double y_t, BlockType type_t) : base(x_t, y_t)
        {
            type = ObjType.Block;
            blockType = type_t;
            if (blockType == BlockType.Cooker || blockType == BlockType.Table || blockType == BlockType.RubbishBin) Layer = (int)MapLayer.BlockLayer;
            else Layer = (int)MapLayer.ItemResistBlockLayer;
            Movable = false;
            
            switch (blockType)
            {
                case BlockType.FoodPoint:
                    _dish = (DishType)Program.Random.Next(1, (int)DishType.Size1 - 1);
                    RefreshTime = Convert.ToInt32(ConfigurationManager.AppSettings["FoodPointInitRefreshTime"]);
                    Console.WriteLine("食品刷新：地点（" + Position.x + "," + Position.y + "）, 种类 : " + Dish);
                    lock (Program.MessageToClientLock)
                    {
                        Program.MessageToClient.GameObjectMessageList.Add(
                            this.ID,
                            new GameObjectMessage
                            {
                                ObjType = ObjTypeMessage.Block,
                                BlockType = BlockTypeMessage.FoodPoint,
                                DishType = (DishTypeMessage)Dish,
                                Position = new XYPositionMessage { X = Position.x, Y = Position.y }
                            });
                    }
                    break;
                case BlockType.Cooker:
                    _dish = DishType.Empty;
                    lock (Program.MessageToClientLock)
                    {
                        Program.MessageToClient.GameObjectMessageList.Add(
                            this.ID,
                            new GameObjectMessage
                            {
                                ObjType = ObjTypeMessage.Block,
                                BlockType = BlockTypeMessage.Cooker,
                                DishType = (DishTypeMessage)Dish,
                                Position = new XYPositionMessage { X = Position.x, Y = Position.y }
                            });
                    }
                    break;
                case BlockType.TaskPoint:
                    Task = new List<DishType>();
                    RefreshTimer.Change(1000, Convert.ToInt32(ConfigurationManager.AppSettings["TaskRefreshTime"]));
                    lock (Program.MessageToClientLock)
                    {
                        Program.MessageToClient.GameObjectMessageList.Add(
                            this.ID,
                            new GameObjectMessage
                            {
                                ObjType = ObjTypeMessage.Block,
                                BlockType = BlockTypeMessage.TaskPoint,
                                DishType = (DishTypeMessage)Dish,
                                Position = new XYPositionMessage { X = Position.x, Y = Position.y }
                            });
                    }
                    break;
            }
        }
        public override DishType GetDish(DishType t)
        {
            DishType temp = Dish;
            Dish = DishType.Empty;
            if (this.blockType == BlockType.FoodPoint) RefreshTimer.Change(RefreshTime, 0);
            return temp;
        }
        protected System.Threading.Timer _refreshTimer;
        public System.Threading.Timer RefreshTimer
        {
            get
            {
                if (this.blockType == BlockType.FoodPoint) _refreshTimer = _refreshTimer ?? new System.Threading.Timer(Refresh);
                else if(this.blockType == BlockType.TaskPoint) _refreshTimer = _refreshTimer ?? new System.Threading.Timer(TaskProduce);
                return _refreshTimer;
            }
        }
        public System.Threading.Timer CookingTimer;
        public bool Cooking = false;
        public void Refresh(object i)
        {
            Dish = (DishType)Program.Random.Next(1, (int)DishType.Size1 - 1);
            Console.WriteLine("食品刷新：地点（" + Position.x + "," + Position.y + "）, 种类 : " + Dish);
        }

        public override void UseCooker()
        {
            string Material = "";

            SortedSet<DishType> dishTypeSet = new SortedSet<DishType>();
            foreach (var GameObject in WorldMap.Grid[(int)Position.x, (int)Position.y].GetLayer((int)MapLayer.ItemLayer))
            {
                if (GameObject is Dish)
                { dishTypeSet.Add(((Dish)GameObject).Dish);GameObject.Parent = null; }
            }
            if (dishTypeSet.Count == 0) return;
            Cooking = true;
            foreach (var dishType in dishTypeSet)
            {
                Material += dishType.ToString() + " ";
                
            }
            string result = ConfigurationManager.AppSettings[Material];
            CookingTimer = new System.Threading.Timer(Cook, result, Convert.ToInt32(ConfigurationManager.AppSettings[result + "Time"]), 0);
            DishType GetResult(string s)
            {
                for (int i = 0; i < (int)DishType.Size2; i++)
                {
                    if ((Convert.ToString((DishType)i)) == s) return (DishType)i;
                }
                return DishType.DarkDish;
            }
            void Cook(object s)
            {
                if (s is string)
                { Dish = GetResult((string)s); Cooking = false; }
            }
        }

        public void TaskProduce(object i)
        {
            DishType temp = DishType.Banana;// (DishType)new Random().Next();
            Task.Add(temp);
            //new System.Threading.Timer(remove, temp,
                //Convert.ToInt32(ConfigurationManager.AppSettings["TaskTimeLimit"]), 0);

            void remove(object task)
            {
                if (task is DishType)
                    Task.Remove((DishType)task);
            }
        }

        public override int HandIn(DishType dish_t)
        {
            if (Task.Contains(dish_t))
            {
                Task.Remove(dish_t);
                Console.WriteLine(Convert.ToInt32(ConfigurationManager.AppSettings[dish_t.ToString() + "Score"]));
                return Convert.ToInt32(ConfigurationManager.AppSettings[dish_t.ToString() + "Score"]);//菜品名+Score，在App.config里加
                //测试的时候能直接把食材交进去，比赛的只会产生菜品任务
            }
            return 0;
        }
    }
}
