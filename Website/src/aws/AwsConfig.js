
let config={
    // aws授权信息
    credentials:{
        accessKeyId: process.env.REACT_APP_AccessKeyId,
        secretAccessKey: process.env.REACT_APP_SecretAccessKey
    },
    S3:{
        REGION:process.env.REACT_APP_S3Region,
        BucketName:process.env.REACT_APP_S3BucketName,
        UploadBucketURL:process.env.REACT_APP_UploadBucketApiURL
    },
    DynamoDB:{
        TableName:process.env.REACT_APP_DynamoDBTablename
    }
}

export default config