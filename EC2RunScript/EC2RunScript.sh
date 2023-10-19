#!/bin/bash
set -e 
# 1. 根据 fileTableID 查询 DynamoDb 中 FileTable 对象  
_FileTableID=$1
_S3BucketName=$2
_DynamoDbTableName=$3
_EC2InstancID=$4

echo "下载jq工具..."  
sudo yum -y install jq 
echo "查询 FileTable 中 ID 为$fileTableID 的记录..."  
response=$(aws dynamodb query --table-name ${_DynamoDbTableName} --key-condition-expression "id=:fileTableID" --expression-attribute-values "{\":fileTableID\":{\"S\":\"${_FileTableID}\"}}")
echo "对象为"${response}
# 2. 根据对象中 input_file_path 路径从 S3 下载文件  
input_file_path=$(echo $response | jq -r '.Items[0].input_file_path.S')
base_file_name=$(basename "$input_file_path" .txt)
echo "下载文件名称"${base_file_name}

echo "从 S3 下载文件到临时目录..."  
aws s3 cp s3://${input_file_path} temp.txt

# 3. 修改下载的文件内容（fileContent + ":" +inputText），并重命名为（原文件名.out.txt）  
inputText=$(echo $response | jq -r '.Items[0].input_text.S')  
fileContent=$(cat temp.txt)

echo "修改文件内容并重命名..."
echo "原文件内容:"${inputText}
echo ${fileContent}":"${inputText} > temp.txt  

# 4. 将该文件上传到 S3  
echo "将文件上传到 S3..."  
aws s3 cp temp.txt s3://${_S3BucketName}/${base_file_name}.out.txt

# 5. 更新 fileTableID 查询 DynamoDb 中 FileTable 对象 out_file_path 为 S3 上传文件路径  
echo "更新 DynamoDB 中的 out_file_path..."  
aws dynamodb update-item --table-name ${_DynamoDbTableName} --key "{\"id\": {\"S\": \"${_FileTableID}\"}}" --update-expression "SET out_file_path=:out_file_path" --expression-attribute-values "{\":out_file_path\":{\"S\":\"${_S3BucketName}/${base_file_name}.out.txt\"}}" 
# 6. 终止EC2实例运行
echo "终止EC2实例运行"
aws ec2 terminate-instances --instance-ids ${_EC2InstancID}