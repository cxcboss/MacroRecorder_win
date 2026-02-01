#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
键盘鼠标录制器 - 播放模块
"""

import sys
import json
import time
from pynput.mouse import Button, Controller as MouseController
from pynput.keyboard import Key, Controller as KeyboardController

class Player:
    def __init__(self):
        self.mouse = MouseController()
        self.keyboard = KeyboardController()
        self.playing = False
        self.actions = []
        
    def play(self, actions):
        """播放录制内容"""
        if self.playing or not actions:
            return
            
        self.playing = True
        self.actions = actions
        start_time = time.time()
        
        print("PLAYBACK_STARTED", flush=True)
        
        for action in self.actions:
            if not self.playing:
                break
                
            # 计算等待时间
            target_time = action["time"]
            elapsed = time.time() - start_time
            wait_time = target_time - elapsed
            
            if wait_time > 0:
                time.sleep(wait_time)
            
            # 执行动作
            action_type = action["type"]
            
            if action_type == "mouse_move":
                self.mouse.position = (action["x"], action["y"])
                
            elif action_type == "mouse_click":
                button = Button.left if action["button"] == "Button.left" else Button.right
                if action["pressed"]:
                    self.mouse.press(button)
                else:
                    self.mouse.release(button)
                    
            elif action_type == "mouse_scroll":
                self.mouse.scroll(action["dx"], action["dy"])
                
            elif action_type == "key_press":
                key = self.parse_key(action["key"])
                if key:
                    self.keyboard.press(key)
                    
            elif action_type == "key_release":
                key = self.parse_key(action["key"])
                if key:
                    self.keyboard.release(key)
        
        self.playing = False
        print("PLAYBACK_STOPPED", flush=True)
    
    def stop(self):
        """停止播放"""
        self.playing = False
        
    def parse_key(self, key_str):
        """解析键名"""
        if not key_str:
            return None
            
        # 特殊键处理
        if key_str.startswith("Key."):
            key_name = key_str[4:]
            if hasattr(Key, key_name.lower()):
                return getattr(Key, key_name.lower())
        elif key_str.startswith("'") and key_str.endswith("'"):
            return key_str[1:-1]
        else:
            return key_str
        
        return None

def main():
    player = Player()
    
    for line in sys.stdin:
        line = line.strip()
        
        if line.startswith("PLAY:"):
            try:
                data = json.loads(line[5:])
                actions = data.get("actions", [])
                player.play(actions)
            except json.JSONDecodeError:
                print("ERROR: Invalid JSON", flush=True)
                
        elif line == "STOP":
            player.stop()
        elif line == "QUIT":
            break

if __name__ == "__main__":
    main()
