FROM microsoft/dotnet:2.1-sdk-stretch

RUN apt-get update && apt-get install -y unzip \
        && curl -sSL https://aka.ms/getvsdbgsh | bash /dev/stdin -v latest -l ~/vsdbg
ENV PATH="${PATH}:/root/vsdbg/vsdbg"

RUN apt-get update && apt-get install -y openssh-server \
    && mkdir /var/run/sshd \
    && echo 'root:Passw0rd' | chpasswd \
    && sed -i 's/PermitRootLogin.*/PermitRootLogin yes/' /etc/ssh/sshd_config \
    && sed 's@session\s*required\s*pam_loginuid.so@session optional pam_loginuid.so@g' -i /etc/pam.d/sshd \
    && echo "export VISIBLE=now" >> /etc/profile
ENV NOTVISIBLE "in users profile"

WORKDIR /app
ENTRYPOINT ["bash"]
