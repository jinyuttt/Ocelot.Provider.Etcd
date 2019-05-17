# Ocelot.Provider.Etcd
Ocelot.Provider.Etcd

Install-Package Ocelot.Provider.Etcd.Cluster  

See: https://github.com/ThreeMammals/Ocelot.Provider.Consul  


该版本不同于Ocelot.Provider.Etcd  
区别：  
    1.内部每隔10秒会获取一次节点地址，便于集群使用  
	2.获取服务信息，虽然也同consul,但是只是初始化，此版本利用了etcd的v3特性，采用watch机制监视服务配置，更快反应服务信息
