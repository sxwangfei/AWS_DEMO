import config from "./AwsConfig";
const { S3Client, PutObjectCommand } = require("@aws-sdk/client-s3");
// 文件上传到s3桶
export async function uploadFileToS3(file) {
    const s3 = new S3Client({
        region: config.S3.REGION,
        credentials: config.credentials
    });
    const uploadParams = {
        Bucket: config.S3.BucketName,
        Key: file.name,
        Body: file
    };
    const result= await s3.send(new PutObjectCommand(uploadParams));
    console.log('文件上传成功',result)
    return true
}
