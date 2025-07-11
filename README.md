# 🍔 FastTechFood

O **FastTechFood** é um MVP desenvolvido com arquitetura de microsserviços utilizando .NET 8, RabbitMQ, MongoDB e uma stack moderna de observabilidade com Prometheus, Grafana e Zabbix.

---
## 🚀 Tecnologias Utilizadas

- 🟦 **[.NET 8](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)**
- 🍃 **[MongoDB](https://www.mongodb.com/)**
- 🐇 **[RabbitMQ](https://www.rabbitmq.com/)**
- 🐳 **[Docker & Docker Compose](https://www.docker.com/)**
- 🌐 **[Ocelot API Gateway](https://ocelot.readthedocs.io/en/latest/)**
- 📈 **[Prometheus](https://prometheus.io/)**

---
## ▶️ Como Executar o Projeto

```bash
1. Clone o repositório:
git clone https://github.com/seu-usuario/fasttechfood.git
cd fasttechfood

2. Suba os serviços com Docker Compose:
docker-compose up --build
````
## Acesse os serviços:

- **API:** [http://localhost:5000](http://localhost:5000)
- **Gateway:** [http://localhost:5001](http://localhost:5001)
- **Zabbix Web:** [http://localhost:8080](http://localhost:8080)
- **Grafana:** [http://localhost:3000](http://localhost:3000)
- **Prometheus:** [http://localhost:9090](http://localhost:9090)
---

## 📦 Estrutura de Pastas

```bash
FastTechFood/
├── FastTechFood.API              # Web API principal
├── FastTechFood.API.Gateway     # Gateway Ocelot
├── FastTechFood.Application     # Camada de aplicação (DTOs, services)
├── FastTechFood.Domain          # Domínio (Entidades e interfaces)
├── FastTechFood.Infrastructure  # Persistência (MongoDB)
├── FastTechFood.Messaging       # RabbitMQ (Publisher/Consumer)
├── FastTechFood.Tests           # Testes automatizados
├── docker-compose.yml           # Orquestração dos containers
````
---

## 📐 Arquitetura da Solução

```mermaid
graph TD
    subgraph API
        APIGateway["API Gateway (Ocelot)"]
        FastTechFoodAPI["FastTechFood.API"]
    end

    subgraph Application
        AppLayer["Application Layer"]
        DomainLayer["Domain Layer"]
    end

    subgraph Infrastructure
        InfraLayer["Infrastructure Layer"]
        MongoDB[(MongoDB)]
        RabbitMQ[(RabbitMQ)]
    end

    subgraph Messaging
        MessagingConsumer["RabbitMQ Consumer"]
        MessagingPublisher["RabbitMQ Publisher"]
    end

    subgraph Monitoring
        Prometheus["Prometheus"]
        Zabbix["Zabbix"]
        Grafana["Grafana"]
    end

    APIGateway --> FastTechFoodAPI
    FastTechFoodAPI --> AppLayer
    AppLayer --> DomainLayer
    AppLayer --> MessagingPublisher
    MessagingConsumer --> InfraLayer
    InfraLayer --> MongoDB
    MessagingPublisher --> RabbitMQ
    MessagingConsumer --> RabbitMQ

    Prometheus --> Grafana
    Zabbix --> Grafana
    FastTechFoodAPI --> Prometheus
    FastTechFoodAPI --> Zabbix
