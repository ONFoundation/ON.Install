variable prefix {}
variable location {}
variable do_token {}
variable sshKeyId {}

terraform {
  required_providers {
    digitalocean = {
      source = "digitalocean/digitalocean"
      version = "~> 2.0"
    }
  }
}

provider "digitalocean" {
  token = var.do_token
}

resource "digitalocean_droplet" "vm1" {
  image = "docker-20-04"
  name = "${var.prefix}-vm-1"
  region = var.location
  size = "s-1vcpu-1gb"
  ssh_keys = [
    var.sshKeyId
  ]

  # connection {
  #   host = self.ipv4_address
  #   user = var.username
  #   type = "ssh"
  #   private_key = file("../../ssh.priv")
  #   timeout = "2m"
  # }

  # provisioner "remote-exec" {
  #   inline = [
  #     "export PATH=$PATH:/usr/bin"
  #     # install nginx
  #     #"sudo apt update",
  #     #"sudo apt install -y nginx"
  #   ]
  # }
}