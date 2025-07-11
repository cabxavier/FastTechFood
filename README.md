# FastTechFood

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

