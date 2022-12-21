docker build -t mqtt-verifier -f ./MqttTestValidator/Dockerfile ./MqttTestValidator

docker image tag mqtt-verifier edgeappmodel.azurecr.io/mqtt-verifier:latest

docker push edgeappmodel.azurecr.io/mqtt-verifier:latest