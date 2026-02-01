#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
键盘鼠标录制器 - 录制模块
"""

import sys
import json
import time
import threading
from pynput import mouse, keyboard
from datetime import datetime

class Recorder:
    def __init__(self):
        self.recording = False
        self.actions = []
        self.start_time = None
        self.mouse_listener = None
        self.keyboard_listener = None
        
    def start(self):
        """开始录制"""
        if self.recording:
            return
            
        self.recording = True
        self.actions = []
        self.start_time = time.time()
        
        # 启动鼠标监听
        self.mouse_listener = mouse.Listener(
            on_move=self.on_move,
            on_click=self.on_click,
            on_scroll=self.on_scroll
        )
        self.mouse_listener.start()
        
        # 启动键盘监听
        self.keyboard_listener = keyboard.Listener(
            on_press=self.on_press,
            on_release=self.on_release
        )
        self.keyboard_listener.start()
        
        print("RECORDING_STARTED", flush=True)
        
    def stop(self):
        """停止录制"""
        if not self.recording:
            return []
            
        self.recording = False
        
        if self.mouse_listener:
            self.mouse_listener.stop()
            self.mouse_listener = None
            
        if self.keyboard_listener:
            self.keyboard_listener.stop()
            self.keyboard_listener = None
        
        # 保存录制结果
        result = {
            "start_time": datetime.now().isoformat(),
            "actions": self.actions
        }
        
        print("RECORDING_STOPPED", flush=True)
        print(json.dumps(result), flush=True)
        
        return result
    
    def get_timestamp(self):
        """获取相对时间戳（秒）"""
        return round(time.time() - self.start_time, 4)
    
    def on_move(self, x, y):
        """鼠标移动"""
        if not self.recording:
            return
        self.actions.append({
            "type": "mouse_move",
            "time": self.get_timestamp(),
            "x": x,
            "y": y
        })
    
    def on_click(self, x, y, button, pressed):
        """鼠标点击"""
        if not self.recording:
            return
        self.actions.append({
            "type": "mouse_click",
            "time": self.get_timestamp(),
            "x": x,
            "y": y,
            "button": str(button),
            "pressed": pressed
        })
    
    def on_scroll(self, x, y, dx, dy):
        """鼠标滚轮"""
        if not self.recording:
            return
        self.actions.append({
            "type": "mouse_scroll",
            "time": self.get_timestamp(),
            "x": x,
            "y": y,
            "dx": dx,
            "dy": dy
        })
    
    def on_press(self, key):
        """键盘按下"""
        if not self.recording:
            return
        try:
            key_name = key.char
        except AttributeError:
            key_name = str(key)
        
        self.actions.append({
            "type": "key_press",
            "time": self.get_timestamp(),
            "key": key_name,
            "pressed": True
        })
    
    def on_release(self, key):
        """键盘释放"""
        if not self.recording:
            return
        try:
            key_name = key.char
        except AttributeError:
            key_name = str(key)
        
        self.actions.append({
            "type": "key_release",
            "time": self.get_timestamp(),
            "key": key_name,
            "pressed": False
        })

def main():
    recorder = Recorder()
    
    for line in sys.stdin:
        line = line.strip()
        
        if line == "START":
            recorder.start()
        elif line == "STOP":
            recorder.stop()
        elif line == "QUIT":
            break

if __name__ == "__main__":
    main()
