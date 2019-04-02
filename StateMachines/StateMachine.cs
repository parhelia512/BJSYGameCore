﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;

namespace BJSYGameCore.StateMachines
{
    public abstract class StateMachine : MonoBehaviour
    {
        protected void Awake()
        {
            onAwake();
        }
        protected virtual void onAwake()
        {
            state = getDefaultState();
        }
        protected void Update()
        {
            onUpdate();
        }
        /// <summary>
        /// 调用onTransit方法获取下一个状态，如果为空则不进行转换。如果当前状态为空，则设置为默认状态，并调用当前状态的onUpdate方法。
        /// </summary>
        protected virtual void onUpdate()
        {
            IState nextState = onTransit();
            if (nextState != null)
                state = nextState;
            if (state == null)
                state = getDefaultState();
            if (state != null)
                state.onUpdate();
        }
        protected abstract IState getDefaultState();
        /// <summary>
        /// 默认返回null，不会发生任何状态转换。
        /// </summary>
        /// <returns></returns>
        protected virtual IState onTransit()
        {
            return null;
        }
        public IState state
        {
            get { return getState(); }
            set
            {
                if (state != null)
                    state.onExit();
                setState(value);
                if (state != null)
                    state.onEntry();
            }
        }
        /// <summary>
        /// 用于实现state属性。
        /// </summary>
        /// <returns></returns>
        protected abstract IState getState();
        /// <summary>
        /// 用于实现state属性。
        /// </summary>
        /// <param name="state"></param>
        protected abstract void setState(IState state);
        public abstract IState[] getAllStates();
        public abstract T getState<T>() where T : IState;
    }
    public interface IState
    {
        void onEntry();
        void onUpdate();
        void onExit();
    }
}