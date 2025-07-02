FROM mcr.microsoft.com/dotnet/sdk:latest AS base
USER root
WORKDIR /app

#### Remote Debugging Setup ####
# Update package lists, install openssh-server, curl, and unzip.
RUN apt-get update && \
    apt-get install -y --no-install-recommends openssh-server curl unzip procps net-tools tcpdump && \
    rm -rf /var/lib/apt/lists/* && \
    mkdir /var/run/sshd && \
    # Set root password to "password123!"
    echo 'root:password123!' | chpasswd && \
    # Configure SSH: allow root login and enable password authentication.
    sed -i 's/^#*PermitRootLogin .*/PermitRootLogin yes/' /etc/ssh/sshd_config && \
    sed -i 's/^#*PasswordAuthentication .*/PasswordAuthentication yes/' /etc/ssh/sshd_config && \
    echo "PermitUserEnvironment yes" >> /etc/ssh/sshd_config && \
    # Install vsdbg for remote debugging
    curl -sSL https://aka.ms/getvsdbgsh | bash /dev/stdin -v latest -l /vsdbg && \
    # Prepare SSH environment for root
    mkdir -p /root/.ssh && chmod 700 /root/.ssh
# Expose the SSH port (22) and your application port (e.g. 8080)
EXPOSE 22
EXPOSE 8080
# ENTRYPOINT ["/usr/sbin/sshd"]

# Copy the custom entrypoint script into the container.
COPY ./entrypoint.sh /app/entrypoint.sh
RUN chmod +x /app/entrypoint.sh

# Use our entrypoint script to start SSH
ENTRYPOINT ["/app/entrypoint.sh"]