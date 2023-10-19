const { EC2Client, RunInstancesCommand } = require("@aws-sdk/client-ec2");

exports.handler = async (e, context) => {
    var env = process.env;
    console.log('准备启动EC2实例')
    console.log('环境变量', env)
    console.log('参数', JSON.stringify(e))
    console.log('触发参数 FileTable_id', e.Records[0].dynamodb.Keys.id.S)
    
    var FileTable_id=e.Records[0].dynamodb.Keys.id.S;

    const command = new RunInstancesCommand({
        //Your key pair name.
        //KeyName: "",
        // Your security group.
        SecurityGroupIds: [env.SecurityGroupIds],
        // An x86_64 compatible image.
        ImageId: env.ImageId,
        // An x86_64 compatible free-tier instance type.
        InstanceType: "t3.micro",
        // Ensure only 1 instance launches.
        MinCount: 1,
        MaxCount: 1,
        SubnetId: env.SubnetId,
        IamInstanceProfile:{
            Name: env.EC2RoleName
        },
        UserData: Buffer.from(`#!/bin/bash 
        cd /home/ec2-user
        logfile=ec2.log
        # 
        aws s3api get-object --bucket `+env.BucketName+` --key `+env.EC2ScriptFileName+` EC2RunScript.tar
        echo "download EC2RunScript.tar" >> $logfile
        # 
        sudo tar -xvf EC2RunScript.tar
        echo "tar EC2RunScript.tar" >> $logfile
        
        ec2id=$(ec2-metadata -i)
        array=($\{ec2id//,/ })
        ec2InstanceID=$\{array[1]}
        
        #
        echo "bash start" >> $logfile
        sudo bash EC2RunScript.sh `+FileTable_id+` `+env.BucketName+` `+env.DynamoDbTableName+` $\{ec2InstanceID}
        echo "bash end" >> $logfile 
        `).toString('base64')
    });
    var client = new EC2Client({
        region: env.EC2Region,
    })
    const result = await client.send(command);
    console.log('EC2启动了', result);


    const response = {
        statusCode: 200,
        body: JSON.stringify('ec2启动成功!'),
    };
    return response;
};
