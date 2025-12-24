<template>
	<div class="min-h-screen flex items-center justify-center bg-gray-100">
		<div class="bg-white shadow-md rounded-lg p-8 w-full max-w-md">
			<h2 class="text-2xl font-bold mb-6 text-center text-gray-800">登录测试</h2>
			<form class="space-y-4" @submit.prevent="handleLogin">
				<div>
					<label class="block text-sm font-medium text-gray-700" for="username">用户名</label>
					<input
							id="username"
							v-model="username"
							class="mt-1 block w-full px-4 py-2 border border-gray-300 rounded-md shadow-sm focus:ring-blue-500 focus:border-blue-500"
							required
							type="text"
					/>
				</div>
				<div>
					<label class="block text-sm font-medium text-gray-700" for="password">密码</label>
					<input
							id="password"
							v-model="password"
							class="mt-1 block w-full px-4 py-2 border border-gray-300 rounded-md shadow-sm focus:ring-blue-500 focus:border-blue-500"
							required
							type="password"
					/>
				</div>
				<button
						class="w-full bg-blue-600 text-white py-2 px-4 rounded-md hover:bg-blue-700 transition"
						type="submit"
				>
					登录
				</button>
			</form>

			<div v-if="responseMessage" class="mt-6 text-center text-sm text-gray-700">
				{{ responseMessage }}
			</div>
		</div>
	</div>
</template>

<script setup>
import {ref} from 'vue'
import axios from 'axios'

const username = ref('')
const password = ref('')
const responseMessage = ref('')

const handleLogin = async () => {
	try {
		const res = await axios.post('/vm/home/login', {
			username: username.value,
			password: password.value
		})

		if (res.data?.value?.token) {
			responseMessage.value = `✅ 登录成功，Token: ${res.data.value.token}`
		} else {
			responseMessage.value = `❌ 登录失败：${res.data?.error || '未知错误'}`
		}
	} catch (error) {
		responseMessage.value = `🚫 请求错误：${error.message}`
	}
}
</script>
