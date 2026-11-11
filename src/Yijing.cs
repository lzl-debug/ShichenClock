using System;

namespace ShichenClock {
    // Bit 0 is the bottom line (初爻); 1 = solid yang, 0 = broken yin.
    public sealed class TimeHexagram {
        public readonly string Name, Image;
        public readonly int Number, Bits;
        public TimeHexagram(string name,string image,int number,int bits) {Name=name;Image=image;Number=number;Bits=bits;}
        public bool Yang(int bottomIndex) {return (Bits & (1<<bottomIndex))!=0;}
    }
    public static class Yijing {
        // 十二消息卦 / 十二辟卦, indexed by 子丑寅卯辰巳午未申酉戌亥.
        public static readonly TimeHexagram[] Cycle = {
            new TimeHexagram("复","地雷复",24,1), new TimeHexagram("临","地泽临",19,3),
            new TimeHexagram("泰","地天泰",11,7), new TimeHexagram("大壮","雷天大壮",34,15),
            new TimeHexagram("夬","泽天夬",43,31), new TimeHexagram("乾","乾为天",1,63),
            new TimeHexagram("姤","天风姤",44,62), new TimeHexagram("遁","天山遁",33,60),
            new TimeHexagram("否","天地否",12,56), new TimeHexagram("观","风地观",20,48),
            new TimeHexagram("剥","山地剥",23,32), new TimeHexagram("坤","坤为地",2,0)
        };
        // Earlier Heaven: south at top; clockwise 乾巽坎艮坤震离兑.
        public static readonly int[] EarlierHeaven = {7,6,2,4,0,1,5,3};
        public static readonly string[] TrigramNames = {"坤","震","坎","兑","艮","离","巽","乾"};
        public static TimeHexagram At(ClockMoment moment) {return Cycle[moment.Index];}
        public static int ChangingLine(int index) {
            int difference=Cycle[index].Bits^Cycle[(index+1)%12].Bits;
            for(int i=0;i<6;i++)if(difference==(1<<i))return i;
            throw new InvalidOperationException("A message hexagram must change exactly one line.");
        }
        public static double DayProgress(ClockMoment moment) {return (moment.Index+moment.Progress)/12.0;}
    }
}
