using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Radegast;
using OpenMetaverse;
using OpenMetaverse.Packets;
using OpenMetaverse.Messages.Linden;

namespace Radegast.RegionConsole.cs
{
	
	[Radegast.Plugin(Name="Region/Estate Console", Description="Just a learning project, making a region/estate console", Version="0.1")]
	public class RegionConsole: IRadegastPlugin {
		string version = "0.1";
		private RadegastInstance Instance;
        private GridClient Client { get { return Instance.Client; } }
		
		//method for plugin enabled.
		public void StartPlugin(RadegastInstance inst) {
			Instance = inst;
            Instance.MainForm.TabConsole.DisplayNotificationInChat("Region/Estate Console version: " + version + " now loaded. type '/sim help' in chat for help. ");
			// We want to process incoming chat in this plugin
            Client.Self.ChatFromSimulator += new EventHandler<ChatEventArgs>(Self_ChatFromSimulator);
			
			//my own handle for region config changes
			//Client.Network.RegisterCallback(OpenMetaverse.Packets.PacketType.EstateOwnerMessage, estateUpdateHandle);
		}
		
		//custom handler for dealing with estate packets
		void estateUpdateHandle(object sender, PacketReceivedEventArgs e) {
			EstateOwnerMessagePacket message = (EstateOwnerMessagePacket)e.Packet;
			RegionFlags flag = (RegionFlags)(Utils.BytesToInt(message.ParamList[3].Parameter));
			if((flag & RegionFlags.NoFly) != RegionFlags.None)
				feedback("nofly");
			else
				feedback("fly");
		}
		
		//plugin stopped
		public void StopPlugin(RadegastInstance instance)
        {
			//deregister events
			Client.Self.ChatFromSimulator -= new EventHandler<ChatEventArgs>(Self_ChatFromSimulator);
			//Client.Network.UnregisterCallback(OpenMetaverse.Packets.PacketType.EstateOwnerMessage, estateUpdateHandle);
		}
		
		//listen to chat for commands
		void Self_ChatFromSimulator(object sender, ChatEventArgs e)
        {
            //ensure from self  //if (e.Type != ChatType.Normal || e.SourceType != ChatSourceType.Agent) return;
            if(e.SourceID != Client.Self.AgentID || e.SourceType != ChatSourceType.Agent) return;
            else //pass message to the handler method
			handleMessage(e.Message.ToLower());
		}
		
		//chat handle method
		private void handleMessage(string cin) {
		    string[] commands = cin.Split(' ');
		    //if first command is not sim, end
		    if(commands[0] != "/sim") return;
		    
			//check client IS estatemanager
			if(Client.Network.CurrentSim.IsEstateManager){} else {
				feedback("Error: You are not an estate manager.");
				return ;
			}
			
    	    switch( commands[1] ) {
    	        
				//regions sim version
    	        case "version":
    	            feedback(Client.Network.CurrentSim.SimVersion);
    	        break;
				
    	        //returns cpu ratio
    	        case "cpuratio":
    	            feedback(Convert.ToString(Client.Network.CurrentSim.CPURatio));
    	        break;
    	        
				//sends a message to the sim
    	        case "message":
    	            //if no more commands can not send message
    	            if(commands[2] == "") {feedback("Error: no message defined."); return;}
    	                string cout="";
    	                int max = commands.Length;
    	                //reconstruct string out
    	                for(int i = 2; i < max; i++) {
    	                    cout += commands[i] + " ";
    	                }
    	                Client.Estate.SimulatorMessage(cout);
    	                feedback("Sim message sent.");
    	        break;
				
				//put sim in state of emergency
				case "emergency":
					//Client.Estate.EstateOwnerMessage("estatechangeinfo", new List<string>() { Client.Network.CurrentSim.Name, "0", "0" }); = no public access, require age verification and payment info
					Client.Estate.EstateOwnerMessage("estatechangeinfo", new List<string>() { Client.Network.CurrentSim.Name, "269484032", "0" });
					//disableScripts (Boolean) disableCollisions (Boolean) disablePhysics (Boolean)
					Client.Estate.SetRegionDebug(true,true,true);
					//block terraform, allow fly, disable damage, disable landresell, restrict pushing, disable parcle join/divide, agent limit 10, object bonus 0, mature false
					Client.Estate.SetRegionInfo(true, false, false, false, true, true, 10, 0, false);
					//kick everyone
					Client.Estate.TeleportHomeAllUsers();
					feedback("Sim is now in state of emergancy: scripts, collissions and physics is disabled. terraform disabled, damage disabled, pushing restricted, parcle join/divide disabled, agent limit 10, object bonus 0, not mature");
				break;
				
				//enable disable public access
				case "access":
    				//Client.Estate.EstateOwnerMessage("estatechangeinfo", new List<string>() { Client.Network.CurrentSim.Name, "0", "0" }); = no public access, require age verification and payment info
					//Client.Estate.EstateOwnerMessage("estatechangeinfo", new List<string>() { Client.Network.CurrentSim.Name, "269484032", "0" });
				    if(commands[2] == "public") {
    				    Client.Estate.EstateOwnerMessage("estatechangeinfo", new List<string>() { Client.Network.CurrentSim.Name, "269516800", "0" });
	    				feedback ("Public access enabled.");
				    }
				    else if (commands[2] == "private") {
    				    Client.Estate.EstateOwnerMessage("estatechangeinfo", new List<string>() { Client.Network.CurrentSim.Name, "269484032", "0" }); //0 alters globaltime
	    				feedback ("Public access disabled.");
				    }
				break;
				
    	        //lock a sim down
    	        case "lockdown":
					//turn off public access
    	            Client.Estate.EstateOwnerMessage("estatechangeinfo", new List<string>() { Client.Network.CurrentSim.Name, "269484032", "0" });
					//teleport everyone home
    	            Client.Estate.TeleportHomeAllUsers();
					feedback ("Sim locked down");
    	        break;
					
				case "flags":
                    if(commands[2] == "default")  {
                        Client.Estate.SetRegionInfo(true, false, false, false, true, false, 20, 0, false );
                        feedback("Sim flags set to default settings (see /sim help for more info).");
                    }
                    
                    //checks number of parameters match.
                    else if(commands.Length == 11) {
                        int agentLimit;
                        try {
                             agentLimit = Convert.ToInt32(commands[8]); //agent limit
                        }
                        catch (FormatException e) { feedback("Error: Agent limit was not a float/integer/number."); return;}
                        catch (OverflowException e) { feedback ("Error: Agent limit was set too high");return;}
                        int objectBonus;
                        try {
                             objectBonus = Convert.ToInt32(commands[9]); //object bonus
                        }
                        catch (FormatException e) { feedback("Error: object bonus was not a float/integer/number."); return;}
                        catch (OverflowException e) { feedback ("Error: object bonus was set too high");return;}
                        
                        Client.Estate.SetRegionInfo((commands[2] == "true"), (commands[3] == "true"), (commands[4] == "true"), (commands[5] == "true"), (commands[6] == "true"), (commands[7] == "true"), agentLimit, objectBonus, (commands[10] == "true"));
                        feedback("Sim flags updated.");
                    }
                    else feedback("Error: mismatch number of parameters");
				break;
    	        
    	        /*case "fly":
				    //bool blockTerraform,	bool blockFly,	bool allowDamage,	bool allowLandResell,	bool restrictPushing,	bool allowParcelJoinDivide,	float agentLimit,	float objectBonus,	bool mature
    	        	Client.Estate.SetRegionInfo(true, false, false, false, true, false, 20, 0, true);
					//Client.Estate.RequestInfo();
    	        break;
				
			    case "nofly":
				    Client.Estate.SetRegionInfo(true, true, false, false, true, false, 20, 0, true);
				    //Client.Estate.RequestInfo();
				break;
				
			    case "check":
				Client.Estate.RequestInfo();
				break;*/
    	        
    	        //teleport everyone home
    	        case "removeall":
    	            Client.Estate.TeleportHomeAllUsers();
    	            feedback("Everyone but you has been removed from the estate.");
    	        break;
    	        
    	        case "restart":
    	            if (commands.Length == 2){ //restart
    	                feedback("Sim restarting");
    	                Thread.Sleep(2000);
    	                Client.Estate.RestartRegion();
    	            }
    	            else if(commands[2] == "/a")  { //cancle restart
    	                Client.Estate.CancelRestart();
    	                feedback("Sim restart aborted.");
    	            }
    	            
    	            //TODO: restart with parameters
    	            
    	        break;
    	        
				case "help":
					feedback("sim console help: command - explanation\n" +
						"Usage: /sim [command] \n"+
						"Commands\n"+
						"access public - enables public access\n" +
						"access private - disabled public access\n" +
						"cpuratio - The number of regions sharing the same CPU as this one \n"+
						"flags [bool:blockTerraform, bool:blockFly, bool:allowDamage, bool:allowLandResell, bool:restrictPushing, bool:allowParcelJoinDivide, integer:agentLimit, integer:objectBonus, bool:isMature] - sets region flags, all need to be set at once.\n"+
						"flags default - sets region flags to be: blockTerraforming, allowFly, disableDamage, denyLandResell, restrictPushing, denyParcelJoinDivide, agentLimit=20, objectBonus=0, notMature\n" +
						"help - show help commands in chat.\n" +
						"lockdown - kicks everyone off the sim and sets it to private access\n" +
						"message <message> - send a message to everyone on the sim.\n" +
						"removeall - kicks everyone else from the estate\n"+
						"restart - restarts sim.\n"+
						"restart /a - abort restart.\n"+
						//"restart [int minutes, message] - restart sim in x minutes and send message.\n"+
						"version - returns the current version of the simulator");
				break;
				
    	        default:
    	        break;
    	    }
		}
		
		//method for giving feedback when a command is run
	    private void feedback(string message) {
    	    Instance.MainForm.TabConsole.DisplayNotificationInChat(message);
		}
		
		//for getting Current sim
		private Simulator getSim() {
		    return Client.Network.CurrentSim;
		}
		
	}
}
