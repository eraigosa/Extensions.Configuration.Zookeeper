using Microsoft.Extensions.Configuration;
using Moq;
using org.apache.zookeeper;
using org.apache.zookeeper.data;
using System.Collections.Generic;
using System.Text;
using Xunit;
using zk = org.apache.zookeeper;
using DotNet.Extensions.Configuration.Zookeeper;
using System;

namespace DotNet.Extensions.Configuration.Zookeeper.Test
{
  public class ZookeeperConfigurationTest : IDisposable
  {
    private void CreateAuthKey()
    {
      var zk = new ZooKeeper("localhost:2181", 3000, null);
      List<ACL> mode = ZooDefs.Ids.CREATOR_ALL_ACL;
      zk.addAuthInfo("digest", Encoding.ASCII.GetBytes("app1:app123"));
      var res = zk.createAsync("/testauth", null, mode, CreateMode.PERSISTENT);
      zk.closeAsync().GetAwaiter().GetResult();
    }

    [Fact]
    public void Get_Value_From_Node()
    {
      string usd = "6.35";
      var zk = new ZooKeeper("localhost:2181", 3000, null);
      zk.createAsync("/AccountApp", null, ZooDefs.Ids.OPEN_ACL_UNSAFE ,CreateMode.PERSISTENT).GetAwaiter().GetResult();
      zk.createAsync("/AccountApp/Rate", null, ZooDefs.Ids.OPEN_ACL_UNSAFE, CreateMode.PERSISTENT).GetAwaiter().GetResult();
      zk.createAsync("/AccountApp/Rate/USD", Encoding.UTF8.GetBytes(usd), ZooDefs.Ids.OPEN_ACL_UNSAFE, CreateMode.PERSISTENT).GetAwaiter().GetResult();
      zk.closeAsync().GetAwaiter().GetResult();

      IConfigurationBuilder builder = new ConfigurationBuilder();
      builder.AddZookeeper(option =>
      {
        option.ConnectionString = "localhost:2181";
        option.ConnectionTimeout = 10000;
        option.RootPath = "/AccountApp";
        option.SessionTimeout = 30000;
      });
      var configuration = builder.Build();
      Assert.Equal(usd, configuration["Rate:USD"]);
    }

    [Fact]
    public void Get_Value_From_Node_With_ACL()
    {
      string usd = "110";
      byte[] auth = Encoding.UTF8.GetBytes("user1:pass1");
      var zk = new ZooKeeper("localhost:2181", 3000, null);
      zk.addAuthInfo("digest", auth);
      zk.createAsync("/AccountAppAuth", null, ZooDefs.Ids.CREATOR_ALL_ACL, CreateMode.PERSISTENT).GetAwaiter().GetResult();
      zk.createAsync("/AccountAppAuth/Rate", null, ZooDefs.Ids.CREATOR_ALL_ACL, CreateMode.PERSISTENT).GetAwaiter().GetResult();
      zk.createAsync("/AccountAppAuth/Rate/USD", Encoding.UTF8.GetBytes(usd), ZooDefs.Ids.CREATOR_ALL_ACL, CreateMode.PERSISTENT).GetAwaiter().GetResult();
      zk.closeAsync().GetAwaiter().GetResult();

      IConfigurationBuilder builder = new ConfigurationBuilder();
      builder.AddZookeeper(option =>
      {
        option.ConnectionString = "localhost:2181";
        option.ConnectionTimeout = 10000;
        option.RootPath = "/AccountAppAuth";
        option.SessionTimeout = 30000;
        option.AddAuthInfo("digest", auth);
      });
      var configuration = builder.Build();
      Assert.Equal(usd, configuration["Rate:USD"]);
    }

    [Fact]
    public void Authenticate_With_Invalid_Credentials_Throws_Exception()
    {
      string usd = "110";
      byte[] auth = Encoding.UTF8.GetBytes("user1:pass1");
      var zk = new ZooKeeper("localhost:2181", 3000, null);
      zk.addAuthInfo("digest", auth);
      zk.createAsync("/AccountAppAuth2", null, ZooDefs.Ids.CREATOR_ALL_ACL, CreateMode.PERSISTENT).GetAwaiter().GetResult();
      zk.createAsync("/AccountAppAuth2/Rate", null, ZooDefs.Ids.CREATOR_ALL_ACL, CreateMode.PERSISTENT).GetAwaiter().GetResult();
      zk.createAsync("/AccountAppAuth2/Rate/USD", Encoding.UTF8.GetBytes(usd), ZooDefs.Ids.CREATOR_ALL_ACL, CreateMode.PERSISTENT).GetAwaiter().GetResult();
      zk.closeAsync().GetAwaiter().GetResult();

      IConfigurationBuilder builder = new ConfigurationBuilder();
      builder.AddZookeeper(option =>
      {
        option.ConnectionString = "localhost:2181";
        option.ConnectionTimeout = 10000;
        option.RootPath = "/AccountAppAuth2";
        option.SessionTimeout = 30000;
        option.AddAuthInfo("digest", Encoding.UTF8.GetBytes("user2:pass1"));
      });
      Assert.Throws<org.apache.zookeeper.KeeperException.NoAuthException> (() => {

        var configuration = builder.Build();
        Assert.Equal(usd, configuration["Rate:USD"]);

      });

    }

    [Fact]
    public void Missing_Root_Path_Throws_Exception()
    {
      string usd = "110";
      IConfigurationBuilder builder = new ConfigurationBuilder();
      builder.AddZookeeper(option =>
      {
        option.ConnectionString = "localhost:2181";
        option.ConnectionTimeout = 10000;
        option.RootPath = "/NonExistingPath";
        option.SessionTimeout = 30000;
      });
      Assert.Throws<org.apache.zookeeper.KeeperException.NoNodeException>(() => {

        var configuration = builder.Build();
        Assert.Equal(usd, configuration["Rate:USD"]);

      });

    }

    private zk.ZooKeeper CreateZookeeper()
    {
        zk.ZooKeeper zk;            
        var client = new Mock<zk.ZooKeeper>(MockBehavior.Strict);
        return client.Object;
    }
    public void Dispose()
    {
      var zk = new ZooKeeper("localhost:2181", 3000, null);
      if (zk.existsAsync("/AccountApp").GetAwaiter().GetResult() != null)
      {
        zk.deleteAsync("/AccountApp/Rate/USD").GetAwaiter().GetResult();
        zk.deleteAsync("/AccountApp/Rate").GetAwaiter().GetResult();
        zk.deleteAsync("/AccountApp").GetAwaiter().GetResult();
      }
      byte[] auth = Encoding.UTF8.GetBytes("user1:pass1");
      
      zk.addAuthInfo("digest", auth);

      if (zk.existsAsync("/AccountAppAuth").GetAwaiter().GetResult() != null)
      {
        zk.deleteAsync("/AccountAppAuth/Rate/USD").GetAwaiter().GetResult();
        zk.deleteAsync("/AccountAppAuth/Rate").GetAwaiter().GetResult();
        zk.deleteAsync("/AccountAppAuth").GetAwaiter().GetResult();
      }

      if (zk.existsAsync("/AccountAppAuth2").GetAwaiter().GetResult() != null)
      {
        zk.deleteAsync("/AccountAppAuth2/Rate/USD").GetAwaiter().GetResult();
        zk.deleteAsync("/AccountAppAuth2/Rate").GetAwaiter().GetResult();
        zk.deleteAsync("/AccountAppAuth2").GetAwaiter().GetResult();
      }
      zk.closeAsync().GetAwaiter().GetResult();
    }
  }
}
