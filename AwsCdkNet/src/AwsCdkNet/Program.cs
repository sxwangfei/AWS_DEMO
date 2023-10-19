using Amazon.CDK;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AwsCdkNet
{
    sealed class Program
    {
        public static void Main(string[] args)
        {
            var app = new App();

            ///1、基础资源构建
            new MyStack(app, "MyStack");
            ///2、React站点部署到S3(网站配置修改后可以发布)
            new WebSiteStack(app, "WebSiteStack");
            app.Synth();
        }
    }
}
