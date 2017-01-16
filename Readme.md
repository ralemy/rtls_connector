# RTLS connector for Impinj ItemSense platform

This codebase contains sample data for creating an RTLS solution based on Impinj ItemsSense technology. 
It publishes the final messages through a rabbitmq brocker which needs to be installed and configured 
individually as described bellow.


# Installation

To Install the solution, the following steps have to be taken:

## Install and Setup RabbitMQ Server


A- Install RabbitMQ as explained in https://www.rabbitmq.com/install-windows.html


B- Go to C:\Program Files\RabbitMQ Server\rabbitmq_server-3.6.6\sbin and execute rabbitmqctl.bat status to make sure it is working.


C- make two accounts, one for administartion and one for locate

C.1 - rabbitmqctl.bat add_user impinj It3ms3ns3

C.2 - rabbitmqctl.bat add_user locate demo2016


D- Give the impinj account administartive priviledges

D.1 - rabbitmqctl.bat set_user_tags impinj administrator

D.2 - rabbitmqctl.bat set_permissions -p / impinj ".*" ".*" ".*"


E-Give the locate account read priviledges on locate queues

E.1 - rabbitmqctl.bat set_user_tags locate subscriber

E.2 - rabbitmqctl.bat set_permissions -p / locate "^$" "^S" "^locate.*"


F- test permissions with rabbitmqctl.bat list_permissions


G- test authentication with rabbitmqctl.bat authenticate_user impinj It3ms3ns3 and rabbitmqctl.bat authenticate_user locate demo2016




