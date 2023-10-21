import {uploadFileToS3} from '../aws/S3'
import {Component} from "react";
import {nanoid} from "nanoid";
import config from "../aws/AwsConfig";
let uploadObject= undefined;
let inputText='';
class S3fileupload extends Component {
    constructor() {
        super();
        this.state={
            file:[],
            inputText:''
        }
        this.fileChange=this.fileChange.bind(this);
        this.inputChange=this.inputChange.bind(this);
    }
    //文件上传
    fileChange = (e) => {
        console.log('选择文件了')
        uploadObject=e.target.files[0];
    }
    //文本输入
    inputChange=e=>{
        this.setState({inputtxt:e.target.value});
        inputText=e.target.value;
    }
    //提交事件
    async Submit() {
        if (uploadObject) {
            const formData = new FormData();
            const file = uploadObject;
            for (let i = 0; i < file.length; i++) {
                formData.append('file', file[i]);
            }
            // 1、上传文件到S3
            var isUpdateSuccess = await uploadFileToS3(file);
            if (isUpdateSuccess){
                // 保存的文件对象
                var storeFileObject={
                    "TableName": config.DynamoDB.TableName,
                    "Item": {
                        "id": { "S":nanoid()},// 生成唯一id
                        "input_text":{"S":inputText},
                        "input_file_path":{"S":config.S3.BucketName+'/'+file.name}
                    }
                }
                //console.log('上传对象',storeFileObject)
                // 2、调用api写数据库
                fetch(config.S3.UploadBucketURL, {
                    method: 'POST',
                    mode: 'no-cors',
                    headers: {
                        'Content-Type': 'application/json',
                        'Access-Control-Allow-Origin': '*',
                        'Access-Control-Allow-Methods': 'GET,PUT,POST,DELETE,PATCH,OPTIONS',
                        'Access-Control-Allow-Headers': 'Origin, X-Requested-With, Content-Type, Accept, Authorization'
                    },
                    body:JSON.stringify(storeFileObject)
                })
                    .then(response => {
                        console.log('数据存储成功')
                        console.log('response',response);
                        alert('提交成功');
                    })
                    .then(data => console.log(data))
                    .catch(error => console.error(error))
            }
        }
    }
    render() {
        return(
        <div className="container mx-auto px-4">
            <section className="font-bold">AWS 文件提交</section>
            <div className="flex flex-col space-y-2">
                <div className="flex flex-row space-x-2">
                    <label className=" w-24">Text input</label>
                    <input className="border-solid border" type="text" onChange={this.inputChange}/>
                </div>
                <div className="flex flex-row space-x-2">
                    <label className=" w-24">File input</label>
                    <input className="block w-full text-sm
                      file:py-1  file:ml-3
                      file:rounded-lg hover:file:border-0
                      file:text-sm file:border-0
                      hover:file:bg-cyan-600 hover:file:text-white" type="file" onChange={this.fileChange}/>
                </div>
                <button className="bg-cyan-500 text-white rounded w-40 hover:bg-cyan-600" onClick={this.Submit}>Submit</button>
            </div>
        </div>
        )
    }
}

export default S3fileupload;