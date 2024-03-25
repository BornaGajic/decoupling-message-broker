# Decoupling Message Brokers
This proof-of-concept repository will show how to decouple from message brokers, specifically Mass Transit implementation.

1. [DecouplingMessageBroker](https://github.com/BornaGajic/decoupling-message-broker/tree/main/DecouplingMessageBroker) is the "host" program, it is used to send messages to the "worker".
2. [Logic](https://github.com/BornaGajic/decoupling-message-broker/tree/main/Logic) is a DLL that is shared between programs.
3. [Worker](https://github.com/BornaGajic/decoupling-message-broker/tree/main/Worker) is receving a message from the host program.
4. [Framework](https://github.com/BornaGajic/decoupling-message-broker/tree/main/Framework) is the main DLL that holds the message broker implementation.
5. [Framework.Test](https://github.com/BornaGajic/decoupling-message-broker/tree/main/Framework.Test) is a testing DLL used to test message broker code.

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
