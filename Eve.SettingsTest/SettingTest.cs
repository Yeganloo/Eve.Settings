using Eve.Settings;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Text;
using System.Threading;
using Xunit;

namespace Eve.SettingsTest
{
    public class SettingTest
    {
        private ManagerSettings setting = new ManagerSettings();
        public class test
        {
            public bool Test { get; set; }

            public string Test1 { get; set; }

            public string Test2 { get; set; }

            public string Test3 { get; set; }
        }

        [Fact]
        public void Read()
        {
            SettingsManager manager = new SettingsManager(setting);
            manager.RememberMe(typeof(test), "Test");
            var ss = manager.GetSettings<test>();
            Assert.True(ss.Test);
            Assert.Equal("string", ss.Test1);
        }

        [Fact]
        public void Write()
        {
            SettingsManager manager = new SettingsManager(setting);
            manager.RememberMe(typeof(test), "Test");
            var ss = manager.GetSettings<test>();
            ss.Test2 = "st2";
            manager.SaveSettings(ss);
            var ss2 = manager.GetSettings<test>();
            Assert.Equal("st2", ss2.Test2);
        }

        [Fact]
        public void ChangeAction()
        {
            var rnd = new Random(DateTime.Now.Millisecond);
            bool failed = true;
            SettingsManager manager = new SettingsManager(setting);
            manager.RememberMe(typeof(test), "Test");
            var ss = manager.GetSettings<test>();
            manager.RegisterEvent("Test", (str) =>
            {
                failed = false;
            });
            using (var f = File.Open($"{setting.SettingsFolder}{Path.DirectorySeparatorChar}Test.json", FileMode.Create))
            {
                ss.Test3 = rnd.NextDouble().ToString();
                f.Write(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(ss)));
            }
            Thread.Sleep(6000);
            var s3 = manager.GetSettings<test>();
            Assert.Equal(ss.Test3, s3.Test3);
            Assert.False(failed);
        }

    }
}
