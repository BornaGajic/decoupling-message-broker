# Decoupling Message Brokers
This proof-of-concept repository will show how to decouple from message brokers, specifically Mass Transit implementation.
## Setup
### Github Actions
If you don't want to clone the repository to see the results, you can visit the [Action](https://github.com/BornaGajic/decoupling-message-broker/actions) tab where all [`Framework.Test`](https://github.com/BornaGajic/decoupling-message-broker/tree/main/Framework.Test) tests run (RabbitMq transport).
### In Memory Transport
In memory transport is provided by default, you can test the functionality by running code tests in Visual Studio.
### RabbitMq Transport
If you want to see an example where a message is sent from one console to another you will need RabbitMq.
1. Install [docker](https://www.docker.com/products/docker-desktop/).
2. Run `docker compose -f './docker-compose.yml' up`.
3. Load the project and set startup projects to both `DecouplingMessageBrokers` and `Worker`.
4. Don't forget to stop the container when you finish.
