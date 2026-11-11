using System;

namespace ShichenClock {
    public sealed class ClockMoment {
        public static readonly string[] Names = {"子", "丑", "寅", "卯", "辰", "巳", "午", "未", "申", "酉", "戌", "亥"};
        public static readonly string[] Animals = {"鼠", "牛", "虎", "兔", "龙", "蛇", "马", "羊", "猴", "鸡", "狗", "猪"};
        public static readonly string[] Poems = {"夜半 · 万籁俱寂", "鸡鸣 · 星河欲曙", "平旦 · 长夜将明", "日出 · 晨光初照", "食时 · 人间烟火", "隅中 · 日渐高升", "日中 · 天光正盛", "日昳 · 暑退风来", "晡时 · 云影渐长", "日入 · 暮色四合", "黄昏 · 灯火可亲", "人定 · 好梦将至"};
        public DateTime Now;
        public int Index;
        public double Progress;
        public string Name { get { return Names[Index]; } }
        public string Animal { get { return Animals[Index]; } }
        public string Poem { get { return Poems[Index]; } }
        public string Range { get { return ((Index * 2 + 23) % 24).ToString("00") + ":00 — " + ((Index * 2 + 1) % 24).ToString("00") + ":00"; } }
        public string Half { get { return Now.Hour % 2 == 1 ? "初" : "正"; } }
        public string Quarter { get { return new [] {"初刻", "一刻", "二刻", "三刻"}[Now.Minute / 15]; } }
        public string Detail { get { return Name + Half + " · " + Quarter; } }
        public string Next { get { return Names[(Index + 1) % 12]; } }
        public int MinutesLeft { get { return (int)Math.Ceiling((1 - Progress) * 120); } }
        public double HourHandAngle { get { return Now.TimeOfDay.TotalSeconds / 86400.0 * 360.0 - 90; } }
        public double MinuteHandAngle { get { return (Now.Minute * 60 + Now.Second + Now.Millisecond / 1000.0) / 3600.0 * 360.0 - 90; } }
        public static ClockMoment At(DateTime beijing) {
            int index = ((beijing.Hour + 1) / 2) % 12;
            double seconds = ((beijing.Hour + 1) % 2) * 3600 + beijing.Minute * 60 + beijing.Second + beijing.Millisecond / 1000.0;
            return new ClockMoment {Now = beijing, Index = index, Progress = seconds / 7200.0};
        }
        public static ClockMoment Current() { return At(DateTime.UtcNow.AddHours(8)); }
    }
}
