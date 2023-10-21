const { DynamoDBClient, PutItemCommand }= require("@aws-sdk/client-dynamodb")
const client = new DynamoDBClient();

// 操作DynamoDb的lambda
exports.handler = async (event, context) => {
    let body;
    let statusCode = '200';
    const headers = {
        'Content-Type': 'application/json',
    };

    try {
        switch (event.httpMethod) {          
            case 'POST':
                const command = new PutItemCommand(JSON.parse(event.body));
                body = await client.send(command);
                break;
            
            default:
                throw new Error(`Unsupported method "${event.httpMethod}"`);
        }
    } catch (err) {
        statusCode = '400';
        body = err.message;
    } finally {
        body = JSON.stringify(body);
    }

    return {
        statusCode,
        body,
        headers,
    };
};
