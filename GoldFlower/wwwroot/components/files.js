import * as InstallState from './install-states.js';
import * as ProcessorType from './processor-types.js';

export default {
	props: ['files', 'installer'],
	data() {
		return {
			selected: [],
			InstallState: InstallState,
			ProcessorType: ProcessorType
		};
	},
	template: `
		<div class="row">
			<div class="col">
				<div class="card bg-default shadow">
					<div class="card-header bg-transparent border-0">
						<h3 class="text-white mb-0">NSP(s)</h3>
					</div>
					<div class="table-responsive">
						<table class="table align-items-center table-dark table-flush">
							<thead class="thead-dark">
								<tr>
									<th v-if="isInstalling" style="width: 75px;"></th>
									<th>Name</th>
									<th>Title ID</th>
									<th>Version</th>
									<th>Size</th>
									<th v-if="!isInstalling">Install</th>
									<th v-else>Progress</th>
									<th style="width: 75px;" v-if="!isInstalling && installer.processorType === ProcessorType.Goldleaf"></th>
								</tr>
							</thead>
							<tbody class="list">
								<tr v-for="(item, index) in items" :key="index">
									<td v-if="isInstalling">
										<i v-if="item.state === InstallState.Installing" class="text-white fa fa-spin fa-circle-o-notch"></i>
										<i v-else-if="item.state === InstallState.Finished" class="text-white fa fa-check text-success"></i>
										<i v-else-if="item.state === InstallState.Failed" class="text-white fa fa-close text-danger"></i>
										<i v-else-if="item.state === InstallState.Cancelled" class="text-white fa fa-close text-danger"></i>
										<i v-else="item.state === InstallState.Idle" class="text-white fa fa-ellipsis-h"></i>
									</td>
									<th scope="row">
										<div class="media align-items-center">												
											<div class="media-body">
												<span class="name mb-0 text-sm">{{ item.name }}</span>
											</div>
										</div>
									</th>

									<td>
										{{ getTitleId(item.name) }}
									</td>

									<td>
										{{ getTitleVersion(item.name) }}
									</td>

									<td>
										{{ convertBytesToHumanSize(item.size) }}
									</td>
									
									<td class="text-center" v-if="!isInstalling">
										<label class="custom-toggle-nsp custom-toggle custom-toggle-success">
											<input type="checkbox" @click="onFileClicked(item)" :checked="item.selected">
											<span class="custom-toggle-slider rounded-circle" data-label-off="" data-label-on=""></span>
										</label>
									</td>

									<td class="text-center" v-else>
										<div class="d-flex align-items-center">
											<template v-if="item.state === InstallState.Installing">
												<span class="completion mr-2">{{ installer.progress }}%</span>
												<div>
													<div class="progress">
														<div class="progress-bar bg-success" role="progressbar" :style="installerProgressWidth"></div>
													</div>
												</div>
											</template>
											<template v-else-if="item.state === InstallState.Finished">
												<span class="completion mr-2">100%</span>
												<div>
													<div class="progress">
														<div class="progress-bar bg-success" role="progressbar" style="width: 100%;"></div>
													</div>
												</div>
											</template>
											<template v-else>
												<span class="completion mr-2">0%</span>
												<div>
													<div class="progress">
														<div class="progress-bar bg-success" role="progressbar" style="width: 0%;"></div>
													</div>
												</div>
											</template>
										</div>
									</td>

									<td class="text-center" style="width: 50px;" v-if="!isInstalling && installer.processorType === ProcessorType.Goldleaf">
										<span class="badge badge-warning" v-if="item.selected">{{ selected.findIndex(x => x.name === item.name) + 1 }}</span>
									</td>
								</tr>
								<tr v-if="!files.length">
									<td colspan="6">
										No (.NSP) files found, make sure you search the correct directory.
									</td>
								</tr>
							</tbody>
						</table>							
					</div>
				</div>
			</div>
		</div>
	`,
	methods: {
		convertBytesToHumanSize(bytes, decimals) {
			if (bytes == 0) return '0 Bytes';

			let k = 1024,
				dm = decimals <= 0 ? 0 : decimals || 2,
				sizes = ['Bytes', 'KB', 'MB', 'GB', 'TB', 'PB', 'EB', 'ZB', 'YB'],
				i = Math.floor(Math.log(bytes) / Math.log(k));

			return parseFloat((bytes / Math.pow(k, i)).toFixed(dm)) + ' ' + sizes[i];
		},

		getTitleId(filename) {
			let regExp = /(?<=\[).+?(?=\])/g;
			let matches = filename.match(regExp);

			return matches && matches.length > 0 ? matches[0] : '-';
		},

		getTitleVersion(filename) {
			let regExp = /(?<=\[).+?(?=\])/g;
			let matches = filename.match(regExp);

			if (matches && matches.length > 1) {
				return matches[1].replace('v', '');
			}

			return '-';
		},

		onFileClicked(file) {
			if (!file.selected) {
				this.selected.push(file);
			} else {
				const index = this.selected.findIndex(x => x.name === file.name);
				this.selected.splice(index, 1);
			}

			this.$emit('change', this.selected);

			file.selected = !file.selected;
		}
	},

	computed: {
		isInstalling() {
			return this.installer.status === this.InstallState.Installing || this.installer.status === this.InstallState.AwaitingUserInput;
		},

		items() {
			return this.isInstalling ? this.installer.files : this.files;
		},
		
		installerProgressWidth() {
			return `width: ${this.installer.progress}%;`;
		}
	}
};
