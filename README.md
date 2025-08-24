# PPCP.Api - Backend para Planejamento e Controle de Produção

## Descrição
O `PPCP.Api` é um backend desenvolvido em ASP.NET Core para o gerenciamento de Planejamento e Controle de Produção (PPCP). Ele fornece uma API RESTful para gerenciar entidades como produtos, máquinas, ordens de produção, turnos, eventos e telemetria, além de integrar com um broker MQTT para receber dados em tempo real de máquinas industriais. O projeto utiliza Entity Framework Core com SQLite como banco de dados e MQTTnet para comunicação com dispositivos IoT.

## Estrutura do Projeto
O projeto segue uma arquitetura limpa, dividida em camadas:
- **Controllers**: Contêm os endpoints da API REST (ex.: `ProdutosController`, `MaquinasController`, `OrdensProducaoController`).
- **Services**: Implementam a lógica de negócio (ex.: `PPCPService`, `MqttService`).
- **Data**: Contém o contexto do Entity Framework (`PPCPContext`) para interação com o banco de dados.
- **Models**: Define as entidades do domínio (`Produto`, `Maquina`, `OrdemProducao`, `Evento`, `Telemetria`, etc.).
- **DTOs**: Usados para deserializar payloads MQTT (`TelemetriaPayload`, `EventoPayload`).

## Pré-requisitos
- **.NET SDK**: Versão 8.0
- **SQLite**: Banco de dados leve usado para persistência.
- **Mosquitto**: Broker MQTT para comunicação com dispositivos IoT.
- **Node-RED**: Opcional, para simular envio de mensagens MQTT para testes.
- **Sistema Operacional**: Testado em Debian/Linux, mas compatível com Windows e macOS.

## Instalação
1. **Clone o repositório**:
   ```bash
   git clone https://github.com/seu-usuario/ppcp-mvp.git
   cd ppcp-mvp/backend/PPCP.Api
   ```

2. **Restaure as dependências**:
   ```bash
   dotnet restore
   ```

3. **Configure o banco de dados**:
   - Certifique-se de que a string de conexão está configurada no arquivo `appsettings.json`:
     ```json
     {
       "ConnectionStrings": {
         "DefaultConnection": "Data Source=ppcp.db"
       },
       "Mqtt": {
         "Server": "localhost",
         "Port": "1883"
       }
     }
     ```
   - Aplique as migrações para criar o banco de dados SQLite:
     ```bash
     dotnet ef migrations add InitialCreate
     dotnet ef database update
     ```

4. **Instale o Mosquitto** (se necessário):
   ```bash
   sudo apt-get install mosquitto mosquitto-clients
   sudo systemctl enable mosquitto
   sudo systemctl start mosquitto
   ```

5. **(Opcional) Instale o Node-RED**:
   - Siga as instruções em [Node-RED](https://nodered.org/docs/getting-started/local) para instalar e configurar.
   - Importe um flow Node-RED para publicar mensagens nos tópicos `factory/+/machine/+/telemetry` e `factory/+/machine/+/event`.

## Como Executar
1. **Compile o projeto**:
   ```bash
   dotnet build
   ```

2. **Execute o backend**:
   ```bash
   dotnet run
   ```
   - A API estará disponível em `https://localhost:5001` (ou a porta configurada).
   - A documentação Swagger estará disponível em `https://localhost:5001/swagger`.

3. **Teste a integração MQTT**:
   - Certifique-se de que o Mosquitto está rodando (`sudo systemctl status mosquitto`).
   - Use o Node-RED ou um cliente MQTT (como `mosquitto_pub`) para publicar mensagens nos tópicos:
     ```bash
     mosquitto_pub -h localhost -t "factory/site1/machine/1/telemetry" -m '{"Timestamp":"2025-08-24T17:00:00Z","State":0,"TotalCount":100,"GoodCount":95,"ScrapCount":5,"Speed_uph":3600,"OpId":1}'
     ```

## Endpoints da API
Os principais endpoints disponíveis na API incluem:
- **Produtos**: `GET /api/Produtos`, `POST /api/Produtos`, `PUT /api/Produtos/{id}`, `DELETE /api/Produtos/{id}`
- **Máquinas**: `GET /api/Maquinas`, `POST /api/Maquinas`, `PUT /api/Maquinas/{id}`, `DELETE /api/Maquinas/{id}`
- **Ordens de Produção**: `GET /api/OrdensProducao`, `POST /api/OrdensProducao`, `PUT /api/OrdensProducao/{id}`, `DELETE /api/OrdensProducao/{id}`
- **KPIs**: `GET /api/KPI` (para métricas como OEE, disponibilidade, performance, qualidade)

Consulte a documentação Swagger (`/swagger`) para detalhes completos dos endpoints.

## Integração MQTT
O serviço `MqttService` subscreve os tópicos:
- `factory/+/machine/+/telemetry`: Recebe dados de telemetria (estado, contagem, velocidade, etc.).
- `factory/+/machine/+/event`: Recebe eventos (início/fim de OP, paradas, etc.).

As mensagens são processadas e armazenadas no banco de dados, atualizando ordens de produção e calculando ETAs automaticamente.

## Estrutura do Banco de Dados
As entidades principais são:
- **Produto**: Representa itens produzidos (código, descrição, ciclo ideal).
- **Maquina**: Equipamentos de produção (código, capacidade nominal, eficiência alvo).
- **Turno**: Períodos de trabalho (nome, início, fim).
- **OrdemProducao**: Ordens de produção (produto, máquina, quantidade planejada, status).
- **Evento**: Eventos relacionados a máquinas (tipo, motivo, timestamps).
- **Telemetria**: Dados em tempo real das máquinas (estado, contagens, velocidade).

## Testes
- **Testes unitários**: Ainda não implementados. Considere adicionar testes usando xUnit ou NUnit.
- **Testes de integração**: Use o Node-RED para simular mensagens MQTT e verificar o processamento no backend.
- **Testes manuais**:
  - Acesse o Swagger (`/swagger`) para testar os endpoints REST.
  - Publique mensagens MQTT usando `mosquitto_pub` ou Node-RED e verifique os logs do backend.

