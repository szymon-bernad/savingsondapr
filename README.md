# SavingsOnDapr

Repository with codebase for "Savings on Dapr" project. Visit https://szymonbernad.substack.com/ for a free access to "Microservices with Dapr" series.

# Running solution with Docker Compose

You should be able to run all services with Docker Compose (I'm using the built-in support in Visual Studio). 
The only prerequisite is creating a volume `pgdata` that is used by postgres service (in Docker Desktop or using `docker volume create pgdata`).

