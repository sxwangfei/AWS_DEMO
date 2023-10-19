using Amazon.CDK;
using Amazon.CDK.AWS.APIGateway;
using Amazon.CDK.AWS.DynamoDB;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.Lambda.EventSources;
using Amazon.CDK.AWS.S3;
using Amazon.CDK.AWS.S3.Deployment;
using Amazon.CDK.AWS.SES.Actions;
using Constructs;
using System.Collections.Generic;
using System.Configuration;

namespace AwsCdkNet
{
    /// <summary>
    /// 基础部署
    /// </summary>
    public class MyStack : Stack
    {
        /// <summary>
        /// S3 Bucket名称
        /// </summary>
        private string _S3BucketName = ConfigurationManager.AppSettings["_S3BucketName"];
        /// <summary>
        /// EC2 执行脚本文件名称(需tar压缩上传)
        /// </summary>
        private string _EC2ScriptFileName = ConfigurationManager.AppSettings["_EC2ScriptFileName"];
        /// <summary>
        /// EC2角色名称
        /// </summary>
        private string _EC2RoleName = ConfigurationManager.AppSettings["_EC2RoleName"];
        /// <summary>
        /// EC2 镜像ID
        /// </summary>
        private string _EC2ImageID = ConfigurationManager.AppSettings["_EC2ImageID"];
        /// <summary>
        /// Region
        /// </summary>
        private string _Region = ConfigurationManager.AppSettings["_Region"];

        /// <summary>
        /// DynamoDB 表名称
        /// </summary>
        private string _DynamoDbTableName = ConfigurationManager.AppSettings["_DynamoDbTableName"];

        /// <summary>
        /// EC2安全组名称
        /// </summary>
        private string _EC2Group = ConfigurationManager.AppSettings["_EC2Group"];
        /// <summary>
        /// EC2 VPC名称
        /// </summary>
        private string _VpcName = ConfigurationManager.AppSettings["_VpcName"];

        internal MyStack(Construct scope, string id, IStackProps props = null) : base(scope, id, props)
        {
            #region 1、创建启动EC2实例时使用的资源

            ///创建角色
            Role role = CreateIamRole();
            role.AddToPolicy(new Amazon.CDK.AWS.IAM.PolicyStatement(new PolicyStatementProps()
            {
                Actions = new string[] { "dynamodb:*", "s3:*", "ec2:*" },
                Effect = Effect.ALLOW,
                //Principals = new Amazon.CDK.AWS.IAM.IPrincipal[] { new Amazon.CDK.AWS.IAM.AnyPrincipal() },
                Resources = new string[] { "*" }
            }));


            //创建VPC      
            SubnetConfiguration[] subnetConfigurations = GetSubnetConfigurations();

            //创建VPC子网
            var vpc = GetVpc(subnetConfigurations);

            //创建安全组
            SecurityGroup securityGroup = CreateSecurityGroup(vpc);
            var ec2GroupId = securityGroup.SecurityGroupId;
            //创建入站规则
            securityGroup.AddIngressRule(Peer.AnyIpv4(), Port.Tcp(22), "Allow SSH Access");

            var ec2SubnetId = vpc.PublicSubnets[0].SubnetId;

            new CfnOutput(this, "EC2GroupId", new CfnOutputProps { Value = ec2GroupId });
            new CfnOutput(this, "EC2SubnetId", new CfnOutputProps { Value = ec2SubnetId });

            new CfnOutput(this, "EC2", new CfnOutputProps { Value = "1__EC2 Finish" });
            #endregion

            #region 2、创建S3桶(创建一个存储桶存放上传文件及EC2执行脚本【EC2RunScript】)

            //创建Bucket
            var platdataBucket = new Bucket(this, "MyStackBucket",
                new BucketProps
                {
                    Versioned = false,
                    BucketName = _S3BucketName,
                    PublicReadAccess = false,
                    //BlockPublicAccess = new BlockPublicAccess(new BlockPublicAccessOptions
                    //{
                    //    BlockPublicAcls = false,
                    //    BlockPublicPolicy = false,
                    //    IgnorePublicAcls = false,
                    //    RestrictPublicBuckets = false
                    //}),
                    BlockPublicAccess = BlockPublicAccess.BLOCK_ALL
                });
            //授权
            //platdataBucket.AddToResourcePolicy
            //    (new Amazon.CDK.AWS.IAM.PolicyStatement(new PolicyStatementProps()
            //    {
            //        Actions = new string[] { "s3:GetObject", "s3:GetObjectVersion", "s3:PutObject" },
            //        Effect = Effect.ALLOW,
            //        Principals = new Amazon.CDK.AWS.IAM.IPrincipal[] { new Amazon.CDK.AWS.IAM.AnyPrincipal() },
            //        Resources = new string[] { platdataBucket.BucketArn, $"{platdataBucket.BucketArn}/*" }
            //    }));
            //跨域
            platdataBucket.AddCorsRule(new CorsRule()
            {
                AllowedHeaders = new string[] { "Authorization", "*" },
                AllowedMethods = new Amazon.CDK.AWS.S3.HttpMethods[] { Amazon.CDK.AWS.S3.HttpMethods.PUT, Amazon.CDK.AWS.S3.HttpMethods.POST, Amazon.CDK.AWS.S3.HttpMethods.DELETE },
                AllowedOrigins = new string[] { "*" }
            });
            //上传启动EC2后执行的脚本压缩包（tar格式压缩)
            var deployStack = new BucketDeployment(this, "MyStackECScript", new BucketDeploymentProps
            {
                Sources = new[] {
                    Source.Asset($"../EC2RunScript",new Amazon.CDK.AWS.S3.Assets.AssetOptions()
                        {
                            Exclude= new string[]{ "EC2RunScript.sh" },
                        }
                    )
                    },
                DestinationBucket = platdataBucket,
            });

            //var cfnAccessPoint = new CfnAccessPoint(this, "MyStackBucketAccessPoint", new CfnAccessPointProps
            //{
            //    Bucket = "mystackbucket",

            //    // the properties below are optional
            //    BucketAccountId = "bucketAccountId",
            //    Name = "ec2-accesspoint",

            //    //Policy = policy,
            //    PublicAccessBlockConfiguration = new PublicAccessBlockConfigurationProperty
            //    {
            //        BlockPublicAcls = false,
            //        BlockPublicPolicy = false,
            //        IgnorePublicAcls = false,
            //        RestrictPublicBuckets = false,
            //    },
            //    VpcConfiguration = new CfnAccessPoint.VpcConfigurationProperty
            //    {
            //        VpcId = vpc.VpcId
            //    }
            //});
            var deployUrl = deployStack.DeployedBucket.UrlForObject();
            //var EC2ScriptDowloadUrl = deployUrl + "/EC2RunScript.tar";
            //new CfnOutput(this, "EC2ScriptDowloadUrl", new CfnOutputProps { Value = deployUrl });
            new CfnOutput(this, "S3", new CfnOutputProps { Value = "2__S3 Finish" });
            #endregion

            #region 3、创建Dymanodb资源和Lambda
            //创建Dynamodb 表
            var dynamoTable = new Table(
               this,
               "MyStackFileTable",
               new TableProps()
               {
                   PartitionKey = new Amazon.CDK.AWS.DynamoDB.Attribute()
                   {
                       Name = "id",
                       Type = AttributeType.STRING
                   },
                   TableName = _DynamoDbTableName,
                   Stream = StreamViewType.NEW_IMAGE,
                   RemovalPolicy = RemovalPolicy.DESTROY
               });
            //创建并上传Dymandb增删改 Lamda执行函数文件
            var lambdaFn = new Amazon.CDK.AWS.Lambda.Function(this, "FileUploadLambda", new FunctionProps
            {
                Runtime = Runtime.NODEJS_14_X,
                Code = Amazon.CDK.AWS.Lambda.Code.FromAsset("../FileUploadLamda/index.zip"),
                Handler = "index.handler",
                Timeout = Duration.Seconds(300),
            });


            // 授权Lambda 操作 DynamoDB table权限
            dynamoTable.GrantFullAccess(lambdaFn);

            //创建DynamoDB table事件通知执行的lambda函数用于启动EC2并执行脚本
            var eventLambdaFn = new Function(this, "DynamodbEventLambda", new FunctionProps
            {
                Runtime = Runtime.NODEJS_18_X,
                Code = Amazon.CDK.AWS.Lambda.Code.FromAsset("../StartEC2Lambda/index.zip"),
                Handler = "index.handler",
                Timeout = Duration.Seconds(300),
                Environment = new Dictionary<string, string>
                {
                    { "SecurityGroupIds", ec2GroupId },
                    { "ImageId",GetEC2ImageID() },
                    { "SubnetId",ec2SubnetId},
                    { "BucketName",_S3BucketName},
                    { "EC2ScriptFileName",_EC2ScriptFileName},
                    { "EC2RoleName",_EC2RoleName},
                    { "EC2Region",_Region},
                    { "DynamoDbTableName",_DynamoDbTableName},
                }
            });
            ///DynamoDB table事件触发（Insert新增时触发 启动EC2)
            eventLambdaFn.AddEventSource(new DynamoEventSource(dynamoTable, new DynamoEventSourceProps
            {
                BatchSize = 1,
                Enabled = true,
                StartingPosition = StartingPosition.LATEST,
                Filters = new[] { FilterCriteria.Filter(new Dictionary<string, object>() { { "eventName", FilterRule.IsEqual("INSERT") } }) }
            })); ;
            // 授权Lambda 操作 DynamoDB table权限
            dynamoTable.GrantWriteData(eventLambdaFn);
            // 授权策略
            eventLambdaFn.AddToRolePolicy(new Amazon.CDK.AWS.IAM.PolicyStatement(new PolicyStatementProps()
            {
                Actions = new string[] { "ec2:*", "iam:PassRole", "iam:ListInstanceProfiles" }, //ec2:RunInstances
                Effect = Effect.ALLOW,
                Resources = new string[] { "*" }
            }));


            // 创建API-Gateway用于请求调用
            var api = new RestApi(this,
               "FileUpload-API",
               new LambdaRestApiProps()
               {
                   RestApiName = "FileUpload",
                   Handler = lambdaFn,
                   EndpointTypes = new EndpointType[] { EndpointType.REGIONAL },
               });
            var apiResource = api.Root.AddResource("FileUpload");//
            apiResource.AddMethod("ANY", new LambdaIntegration(lambdaFn));
            AddCorsOptions(apiResource);
            // 生成前端调用地址（用于配置文件设置）
            new CfnOutput(this, "DynamoDb API URL", new CfnOutputProps { Value = api.Url + "FileUpload" });
            new CfnOutput(this, "DynamoDb", new CfnOutputProps { Value = "3__DynamoDb Finish" });
            #endregion

            #region 4、部署React网站(该部分需要单独部署因为涉及到修改配置信息后才能发布）


            #endregion
        }

        #region 公共方法
        private SubnetConfiguration[] GetSubnetConfigurations()
        {
            return new[]
            {
                   new SubnetConfiguration
                   {
                       CidrMask = 24,
                       Name = "MyStackSubnet",
                       SubnetType = SubnetType.PUBLIC
                    }
            };
        }
        /// <summary>
        /// 创建VPC
        /// </summary>
        /// <param name="subnetConfigurations"></param>
        /// <returns></returns>
        private Vpc GetVpc(SubnetConfiguration[] subnetConfigurations)
        {
            return new Vpc(this, "MyStackVPC", new VpcProps
            {
                NatGateways = 0,
                SubnetConfiguration = subnetConfigurations,
                VpcName = _VpcName
            });
        }

        /// <summary>
        /// 创建安全组
        /// </summary>
        /// <param name="vpc"></param>
        /// <returns></returns>
        private SecurityGroup CreateSecurityGroup(Vpc vpc)
        {
            return new SecurityGroup(this, "MyStackEC2Group",
                new SecurityGroupProps
                {
                    SecurityGroupName = _EC2Group,
                    Vpc = vpc,
                    Description = "Allow SSH access on TCP Port 22 ",
                    AllowAllOutbound = true
                });
        }

        /// <summary>
        ///解决跨域问题
        /// </summary>
        /// <param name="apiResource"></param>
        private void AddCorsOptions(Amazon.CDK.AWS.APIGateway.IResource apiResource)
        {
            apiResource.AddMethod("OPTIONS",
              new MockIntegration(new IntegrationOptions()
              {
                  IntegrationResponses = new IIntegrationResponse[]
                {
            new IntegrationResponse()
            {
              StatusCode = "200",
              ResponseParameters = new Dictionary<string, string>()
              {
                {
                  "method.response.header.Access-Control-Allow-Headers",
                  "'Content-Type,X-Amz-Date,Authorization,X-Api-Key,X-Amz-Security-Token,X-Amz-User-Agent'"
                },
                {
                  "method.response.header.Access-Control-Allow-Origin", "'*'"
                },
                {
                  "method.response.header.Access-Control-Allow-Credentials", "'false'"
                },
                {
                  "method.response.header.Access-Control-Allow-Methods", "'OPTIONS,GET,PUT,POST,DELETE'"
                }
              }
            }
                },
                  PassthroughBehavior = PassthroughBehavior.NEVER,
                  RequestTemplates = new Dictionary<string, string>()
                {
                    {
                      "application/json", "{\"statusCode\": 200}"
                    }
                },
              }),
              new MethodOptions()
              {
                  MethodResponses = new IMethodResponse[]
                {
            new MethodResponse()
            {
              StatusCode = "200",
              ResponseParameters = new Dictionary<string, bool>()
              {
                {
                  "method.response.header.Access-Control-Allow-Headers", true
                },
                {
                  "method.response.header.Access-Control-Allow-Methods", true
                },
                {
                  "method.response.header.Access-Control-Allow-Credentials", true
                },
                {
                  "method.response.header.Access-Control-Allow-Origin", true
                }
              }
            }
                }
              });
        }
        /// <summary>
        /// 创建角色
        /// </summary>
        /// <returns></returns>
        private Role CreateIamRole()
        {
            var role = new Role(this, "MyStack_EC2Role", new RoleProps
            {
                AssumedBy = new ServicePrincipal("ec2.amazonaws.com"),
                RoleName = _EC2RoleName,

            });
            /// 为角色定义Profile
            var instanceProfile = new InstanceProfile(this, "MyStack_InstanceProfile", new InstanceProfileProps
            {
                Role = role,
                InstanceProfileName = _EC2RoleName,
                //Path= "/sample/path/",
            });

            return role;
        }
        /// <summary>
        /// 获取启动EC2的镜像
        /// </summary>
        /// <returns></returns>
        private string GetEC2ImageID()
        {
            return _EC2ImageID;
            //var image = new AmazonLinuxImage(new AmazonLinuxImageProps
            //{
            //    Generation = AmazonLinuxGeneration.AMAZON_LINUX_2,
            //    CpuType = AmazonLinuxCpuType.X86_64

            //});

            ////var construct = new Construct()
            //var imageId= image.GetImage(new Construct(this, "AmazonLinuxImage")).ImageId;
            //new CfnOutput(this, "AmazonLinuxImage ImageId", new CfnOutputProps { Value = imageId });
            //return imageId;
        }
        #endregion
    }
}
