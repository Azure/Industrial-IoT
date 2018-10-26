#!/bin/bash -e


stopall() {
  docker-compose down
  list=$(docker ps -aq)
  if [ -n "$list" ]; then
      docker rm -f $list
  fi
}

startall() {
  docker-compose pull
  rm -f nohup.out
  nohup docker-compose up > /dev/null 2>&1&

  # assume http -> https redirect in place.
  ISUP=$(curl -ks http://localhost/ | grep -i "html" | wc -l)
  while [[ "$ISUP" == "0" ]]; do
    echo "Waiting for web site to start..."
    sleep 3
    ISUP=$(curl -ks http://localhost/ | grep -i "html" | wc -l)
  done
  echo "Web site started!"
}

  if [[ "$1" == "--stop" ]]; then
  stopall
elif [[ "$1" == "--logs" ]]; then
  docker-compose logs
elif [[ "$1" == "--status" ]]; then
  docker-compose ps
elif [[ "$1" == "--stats" ]]; then
  docker stats -a
elif [[ "$1" == "--start" ]]; then
  stopall
  startall
else
  echo "--stop    stop all services"
  echo "--start   start all services"
  echo "--status  print status"
  echo "--stats   print docker stats"
  echo "--logs    show all logs"
fi
