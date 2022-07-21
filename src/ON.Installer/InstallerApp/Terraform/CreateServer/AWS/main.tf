variable prefix {}
variable location {}
variable username {}
variable sshPub {}

terraform {
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 3.27"
    }
  }

  required_version = ">= 0.14.9"
}

provider "aws" {
  profile = "default"
  region  = var.location
  #access_key = AWS Generated Access Key
  #secret_key = AWS Generated Secret Key
}

resource "aws_instance" "vm1" {
  ami           = "ami-03a0c45ebc70f98ea"
  instance_type = "t2.micro"
  key_name = aws_key_pair.deployer.key_name
  network_interface {
    network_interface_id = aws_network_interface.nic1.id
    device_index         = 0
    delete_on_termination = false
  }
  tags = {
    Name = "${var.prefix}-vm-1"
  }
}

resource "aws_key_pair" "deployer" {
  key_name   = "${var.prefix}-deployer-key"
  public_key = var.sshPub
}

resource "aws_vpc" "vpc1" {
  cidr_block = "10.20.20.0/25"
  tags = {
    "Name" = "${var.prefix}-vpc-1"
  }
}

resource "aws_vpc_ipv4_cidr_block_association" "secondary_cidr" {
  vpc_id     = aws_vpc.vpc1.id
  cidr_block = "172.2.0.0/16"
}

resource "aws_subnet" "in_secondary_cidr" {
  vpc_id     = aws_vpc_ipv4_cidr_block_association.secondary_cidr.vpc_id
  cidr_block = "172.2.0.0/24"
}

resource "aws_subnet" "public" {
  vpc_id            = aws_vpc.vpc1.id
  cidr_block        = "10.20.20.64/26"
  availability_zone = "${var.location}b"
  tags = {
    "Name" = "${var.prefix}-public-1"
  }
}

resource "aws_route_table" "vpc1-rt" {
  vpc_id = aws_vpc.vpc1.id
  tags = {
    "Name" = "${var.prefix}-route-table-1"
  }
}

resource "aws_route_table_association" "public" {
  subnet_id      = aws_subnet.public.id
  route_table_id = aws_route_table.vpc1-rt.id
}

resource "aws_internet_gateway" "vpc1-igw" {
  vpc_id = aws_vpc.vpc1.id
  tags = {
    "Name" = "${var.prefix}-gateway-1"
  }
}

resource "aws_route" "internet-route" {
  destination_cidr_block = "0.0.0.0/0"
  route_table_id         = aws_route_table.vpc1-rt.id
  gateway_id             = aws_internet_gateway.vpc1-igw.id
}

resource "aws_network_interface" "nic1" {
  subnet_id       = aws_subnet.public.id
  private_ips     = ["10.20.20.120"]
  security_groups = [aws_security_group.sg_ssh.id]
  tags = {
    "Name" = "${var.prefix}-nic-1"
  }
}

resource "aws_eip" "ip-one" {
  vpc                       = true
  network_interface         = aws_network_interface.nic1.id
  tags = {
    "Name" = "${var.prefix}-ip-1"
  }
}

resource "aws_security_group" "sg_ssh" {
  name = "${var.prefix}-security-group"
  description = "allow inbound traffic"
  vpc_id = aws_vpc.vpc1.id

  #Incoming traffic
  ingress {
    from_port = 80
    to_port = 80
    protocol = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
  }

  ingress {
    from_port = 22
    to_port = 22
    protocol = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
    #cidr_blocks = ["11.xx.xx.xx/32"]
  }

  ingress {
    from_port = 443
    to_port = 443
    protocol = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
  }

  ingress {
    from_port = 8
    to_port = 0
    protocol = "icmp"
    cidr_blocks = ["0.0.0.0/0"]
  }

  #Outgoing traffic
  egress {
    from_port = 0
    protocol = "-1"
    to_port = 0
    cidr_blocks = ["0.0.0.0/0"]
  }
}

resource "aws_subnet" "private" {
  vpc_id            = aws_vpc.vpc1.id
  cidr_block        = "10.20.20.0/26"
  availability_zone = "${var.location}b"
  tags = {
    "Name" = "${var.prefix}-private-1"
  }
}

resource "aws_route_table_association" "private" {
  subnet_id      = aws_subnet.private.id
  route_table_id = aws_route_table.vpc1-rt.id
}