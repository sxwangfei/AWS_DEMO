using Amazon.CDK;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.S3;
using Amazon.CDK.AWS.S3.Deployment;
using Constructs;
using System.Configuration;

namespace AwsCdkNet
{
    /// <summary>
    /// 网站部署
    /// </summary>
    public class WebSiteStack : Stack
    {

        /// <summary>
        /// 网站 Bucket名称
        /// </summary>
        private string _WebSiteBucketName = ConfigurationManager.AppSettings["_WebSiteBucketName"];

        internal WebSiteStack(Construct scope, string id, IStackProps props = null) : base(scope, id, props)
        {
            //创建托管网站的Bucket
            var websiteBucket = new Bucket(this, "WebSiteStack",
                new BucketProps
                {
                    Versioned = false,
                    BucketName = _WebSiteBucketName,
                    PublicReadAccess = true,
                    BlockPublicAccess = new BlockPublicAccess(new BlockPublicAccessOptions
                    {
                        BlockPublicAcls = false,
                        BlockPublicPolicy = false,
                        IgnorePublicAcls = false,
                        RestrictPublicBuckets = false
                    }),
                    WebsiteIndexDocument = "index.html"

                });
            //授权
            websiteBucket.AddToResourcePolicy
                (new Amazon.CDK.AWS.IAM.PolicyStatement(new PolicyStatementProps()
                {
                    Actions = new string[] { "s3:GetObject" },
                    Effect = Effect.ALLOW,
                    Principals = new Amazon.CDK.AWS.IAM.IPrincipal[] { new Amazon.CDK.AWS.IAM.AnyPrincipal() },
                    Resources = new string[] { websiteBucket.BucketArn, $"{websiteBucket.BucketArn}/*" }
                })
           );
            //跨域解决
            websiteBucket.AddCorsRule(new CorsRule()
            {
                AllowedHeaders = new string[] { "Authorization", "*" },
                AllowedMethods = new Amazon.CDK.AWS.S3.HttpMethods[] { Amazon.CDK.AWS.S3.HttpMethods.PUT, Amazon.CDK.AWS.S3.HttpMethods.POST, Amazon.CDK.AWS.S3.HttpMethods.DELETE },
                AllowedOrigins = new string[] { "*" }
            });
            //部署网站
            var webSitedeployStack = new BucketDeployment(this, "WebSiteDeploy", new BucketDeploymentProps
            {
                Sources = new[] { Source.Asset("../Website/build") },
                DestinationBucket = websiteBucket,
            });
            var websiteDeployUrl = webSitedeployStack.DeployedBucket.BucketWebsiteUrl;
            //生成网站访问地址
            new CfnOutput(this, "WebSiteUrl", new CfnOutputProps { Value = $"{websiteDeployUrl}/index.html",Description="网站访问地址" });
            new CfnOutput(this, "Website", new CfnOutputProps { Value = "4__Website Finish" , Description = "网站部署完成" });
        }

    }
}
